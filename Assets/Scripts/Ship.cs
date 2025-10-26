using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Ship : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public Image image;
    public ShipData shipData; // ScriptableObject reference
    public bool isPlaced = false;
    public Vector2Int gridPosition; // starting cell coordinate
    public bool isVertical = false;

    // UI rotate button that rotates the ship when clicked. Enabled only when placed on a grid.
    public UnityEngine.UI.Button rotateButton;

    // allow re-positioning placed ships by dragging
    public bool allowReposition = false;

    // occupied cells when placed
    public Cell[] OccupiedCells { get; private set; }

    // Returns true if all occupied cells are hit
    public bool IsSunk()
    {
        if (OccupiedCells == null || OccupiedCells.Length == 0) return false;
        foreach (var c in OccupiedCells)
        {
            if (c == null || c.State != CellState.Hit) return false;
            
        }
        return true;
    }
    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 originalPosition;

    // currently dragging ship (used by cells for preview)
    public static Ship DraggingShip { get; private set; }
    // currently selected ship in the editor/UI
    public static Ship SelectedShip { get; private set; }

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (image == null)
            image = GetComponentInChildren<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Wire up rotate button if present. Start disabled until ship is placed.
        if (rotateButton != null)
        {
            rotateButton.onClick.AddListener(() => Rotate());
            rotateButton.interactable = false;
        }
    }

    void Update()
    {
        // allow rotation while dragging by pressing R
        if (DraggingShip == this && Input.GetKeyDown(KeyCode.R))
        {
            Rotate();
            // refresh preview on the current preview grid if present
            if (GridController.CurrentPreviewGrid != null)
            {
                GridController.CurrentPreviewGrid.UpdatePreviewForCurrentStart(this);
            }
        }
    }

    /// <summary>
    /// Initialize the ship visuals and data from a ShipData asset.
    /// cellSize and spacing are used to size the RectTransform so ships will align with a grid layout.
    /// </summary>
    private float currentCellSize;
    private float currentSpacing;

    public void Initialize(ShipData data, float cellSize = 10f, float spacing = 0f)
    {
        shipData = data;
        currentCellSize = cellSize;
        currentSpacing = spacing;

        if (image != null && data != null)
        {
            image.sprite = data.shipSprite;
            image.SetNativeSize();
        }

        name = data != null ? data.shipName : "Ship";

        if (rect != null && data != null)
        {
            UpdateShipSize();
        }

        // Ensure rotation matches current orientation
        ApplyRotation();
    }

    private void UpdateShipSize()
    {
        if (shipData == null || rect == null) return;

        float length = shipData.length * currentCellSize + (shipData.length - 1) * currentSpacing;
       
            rect.sizeDelta = new Vector2(length, currentCellSize);
        
    }

    public void PlaceOnGrid(Cell[] cells)
    {
        if (cells == null || cells.Length == 0) return;

        OccupiedCells = cells;
        isPlaced = true;

        // Remember starting coordinate as the first occupied cell
        gridPosition = OccupiedCells[0].coordinate;

        foreach (var c in OccupiedCells)
            c.SetShip();

        // Enable rotate button now that the ship is on the grid
        if (rotateButton != null)
            rotateButton.interactable = true;
    }

    public void OnShipSunk()
    {
        image.enabled = true; 
        image.sprite = shipData.SunkShipSprite;
        foreach (var c in OccupiedCells)
            {
            c.background.enabled = false;
            c.icon.enabled = false;
            }
        // Additional logic when ship is sunk can be added here
    }
    public void RemoveFromGrid()
    {
        if (OccupiedCells != null)
        {
            foreach (var c in OccupiedCells)
            {
                if (c != null) c.ClearShip();
            }
        }
        OccupiedCells = null;
        isPlaced = false;

        // Disable rotate button when not on a grid
        if (rotateButton != null)
            rotateButton.interactable = false;
    }

    public void Rotate()
    {
        // If the ship isn't placed, just toggle orientation visually (editor/spawner behaviour)
        if (!isPlaced)
        {
            isVertical = !isVertical;
            image.SetNativeSize();
            UpdateShipSize(); // Update size before rotation
            ApplyRotation();
            // refresh preview on the current preview grid if present
            if (GridController.CurrentPreviewGrid != null)
            {
                GridController.CurrentPreviewGrid.UpdatePreviewForCurrentStart(this);
            }
            return;
        }

        // When placed on a grid, attempt an in-place rotation that updates occupied cells.
        if (OccupiedCells == null || OccupiedCells.Length == 0 || shipData == null)
            return;

        var grid = OccupiedCells[0].GetComponentInParent<GridController>();
        if (grid == null)
        {
            // fallback to simple rotate if we can't find the grid
            isVertical = !isVertical;
            image.SetNativeSize();
            UpdateShipSize();
            ApplyRotation();
            return;
        }

        bool newIsVertical = !isVertical;
        int len = shipData.length;
        var newCells = new System.Collections.Generic.List<Cell>();

        for (int i = 0; i < len; i++)
        {
            Vector2Int coord = !newIsVertical ? new Vector2Int(gridPosition.x, gridPosition.y + i) : new Vector2Int(gridPosition.x + i, gridPosition.y);
            Cell c = grid.GetCell(coord);
            if (c == null)
            {
                Debug.Log("Rotation blocked: out of bounds");
                return; // can't rotate because it would go out of grid
            }

            // Allow occupying current ship's cells (they will be cleared and re-set). But if another ship occupies the cell -> blocked.
            bool occupiedByThisShip = System.Array.Exists(OccupiedCells, oc => oc == c);
            if (c.State == CellState.Ship && !occupiedByThisShip)
            {
                Debug.Log("Rotation blocked: collision with another ship");
                return; // collision
            }

            newCells.Add(c);
        }

        // Clear previous cells
        foreach (var c in OccupiedCells)
            if (c != null) c.ClearShip();

        // Set the new cells as occupied
        foreach (var c in newCells)
            c.SetShip();

        OccupiedCells = newCells.ToArray();
        isVertical = newIsVertical;

        // Update stored starting grid position to the new first cell
        gridPosition = OccupiedCells[0].coordinate;

        // Re-anchor and resize the ship rect to match the new cells (similar to GridController.TryPlaceShip)
        RectTransform shipRect = GetComponent<RectTransform>();
        Cell firstCell = OccupiedCells.Length > 0 ? OccupiedCells[0] : null;
        Cell lastCell = OccupiedCells.Length > 0 ? OccupiedCells[OccupiedCells.Length - 1] : null;
        if (shipRect != null && firstCell != null && lastCell != null)
        {
            RectTransform firstRect = firstCell.GetComponent<RectTransform>();
            RectTransform lastRect = lastCell.GetComponent<RectTransform>();

           shipRect.SetParent(grid.transform, false);

            float cellW = firstRect.rect.width;
            float cellH = firstRect.rect.height;
            shipRect.sizeDelta = new Vector2(len * cellW, cellH);

            Vector2 center = (firstRect.anchoredPosition + lastRect.anchoredPosition) * 0.5f;
            shipRect.pivot = new Vector2(0.5f, 0.5f);
            shipRect.anchoredPosition = center;

            transform.rotation = Quaternion.Euler(0, 0, isVertical ? 90f : 0f);
        }

        ApplyRotation();
    }

    private void ApplyRotation()
    {
        if (rect == null) return;

        // Reset rotation and pivot
        transform.rotation = Quaternion.identity;
        rect.pivot = new Vector2(0.5f, 0.5f);

        // Apply rotation
        transform.rotation = Quaternion.Euler(0, 0, isVertical ? 90f : 0f);
    }

    // Drag handlers
    private Vector3 dragOffset;
    private Canvas parentCanvas;
    private Camera canvasCamera;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced && !allowReposition) return;
        if (isPlaced && allowReposition)
        {
            RemoveFromGrid();
        }

        // Cache canvas and camera references
        parentCanvas = GetComponentInParent<Canvas>();
        canvasCamera = parentCanvas.worldCamera;

        DraggingShip = this;
        originalParent = transform.parent;
        originalPosition = transform.position;

        // Calculate offset for smooth dragging
        RectTransformUtility.ScreenPointToWorldPointInRectangle(rect, eventData.position, canvasCamera, out Vector3 worldPoint);
        dragOffset = transform.position - worldPoint;

        // Move to canvas but maintain world position
        Vector3 worldPos = transform.position;
        transform.SetParent(parentCanvas.transform, false);
        transform.position = worldPos;

        // Ensure proper Z position for visibility
        Vector3 pos = transform.position;
        pos.z = 0;
        transform.position = pos;
 canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        // Convert screen point to world point
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rect, eventData.position, canvasCamera, out Vector3 worldPoint))
        {
            transform.position = new Vector3(worldPoint.x + dragOffset.x, 
                                          worldPoint.y + dragOffset.y, 
                                          0);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlaced)
        {
            canvasGroup.blocksRaycasts = true;
            DraggingShip = null;
            return;
        }

        // If not placed by drop target, return to original parent/position
        if (!isPlaced)
        {
            Vector3 worldPos = originalPosition;
            transform.SetParent(originalParent, false);
            transform.position = worldPos;
            
            // Ensure proper Z position
            Vector3 pos = transform.position;
            pos.z = originalPosition.z;
            transform.position = pos;
        }

        canvasGroup.blocksRaycasts = true;
        DraggingShip = null;
    }

    // Called by GridController when the ship has been successfully placed
    public void OnPlaced()
    {
        isPlaced = true;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Select this ship for editor-key rotation convenience
        if (SelectedShip == this) SelectedShip = null; else SelectedShip = this;
    }

    public void DestroyShip()
    {
        if (OccupiedCells != null)
        {
            foreach (var c in OccupiedCells)
            {
                // Reset visuals if needed
                if (c != null) c.ClearShip();
            }
        }
        Destroy(gameObject);
    }
}

public class ShipVisibilityManager
{
    public static void UpdateShipVisibility(ShipSpawner spawner, bool isVisible)
    {
        if (spawner != null && spawner.spawnedShips != null)
        {
            foreach (var ship in spawner.spawnedShips)
            {
                if (ship != null && ship.image != null)
                {
                    // Only show unsunk ships if visible is true
                    if (isVisible)
                    {
                        ship.image.enabled = true;
                    }
                    else
                    {
                        // When hiding, only show if ship is sunk
                        ship.image.enabled = ship.IsSunk();
                        foreach (var c in ship.OccupiedCells)
                        {
                            if (c != null && ship.image.enabled)
                            {
                                // Reset cell visuals when hiding ships
                                c.hitEffect.gameObject.SetActive(false);
                            }  
                             }
                    }
                }
            }
        }
    }
}

