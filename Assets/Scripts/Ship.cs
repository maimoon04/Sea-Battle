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
        if (isVertical)
        {
            rect.sizeDelta = new Vector2(currentCellSize, length);
        }
        else
        {
            rect.sizeDelta = new Vector2(length, currentCellSize);
        }
    }

    public void PlaceOnGrid(Cell[] cells)
    {
        OccupiedCells = cells;
        isPlaced = true;
        foreach (var c in OccupiedCells)
            c.SetShip();
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
    }

    public void Rotate()
    {
        isVertical = !isVertical;
        image.SetNativeSize();
        UpdateShipSize(); // Update size before rotation
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
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced && !allowReposition) return; // don't allow dragging placed ships unless allowed
        if (isPlaced && allowReposition)
        {
            // free up currently occupied cells so preview/collision detect uses empty cells
            RemoveFromGrid();
        }

        DraggingShip = this;
        originalParent = transform.parent;
        originalPosition = transform.position;

        // move to top-level canvas so it renders above other UI
        Canvas c = GetComponentInParent<Canvas>();
        if (c != null)
            transform.SetParent(c.transform, true);

        canvasGroup.blocksRaycasts = false; // allow raycasts to go through to cells
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPlaced) return;
        // follow pointer
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlaced)
        {
            DraggingShip = null;
            return;
        }

        // If not placed by drop target, return to original parent/position
        if (!isPlaced)
        {
            transform.SetParent(originalParent, true);
            transform.position = originalPosition;
        }

        canvasGroup.blocksRaycasts = true;
        DraggingShip = null;
    }

    // Called by GridController when the ship has been successfully placed
    public void OnPlaced(Transform parent, Vector2 anchoredPosition)
    {
        isPlaced = true;
        transform.SetParent(parent, false);
        if (rect != null)
            rect.anchoredPosition = anchoredPosition;
        canvasGroup.blocksRaycasts = true;
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
                    }
                }
            }
        }
    }
}

