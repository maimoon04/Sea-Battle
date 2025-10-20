using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;

public class GridController : MonoBehaviour
{
	[Header("Grid Settings")]
	public int columns = 10;
	public int rows = 10;
	public GameObject cellPrefab;
	public RectTransform gridArea;
	public bool isOpponentGrid = false;

	public UnityEvent<Vector2Int> OnCellClicked; // callback for clicks

	private Cell[,] cells;
	private GridLayoutGroup gridLayout;

	// track previewed cells so we can clear them
	private List<Cell> previewCells = new List<Cell>();

	// remember last preview start so orientation-only updates can reapply preview
	private Vector2Int lastPreviewStart;
	private bool hasPreviewStart = false;

	// the grid that currently has an active preview (null if none)
	public static GridController CurrentPreviewGrid { get; private set; }

	void Awake()
	{
		gridLayout = GetComponent<GridLayoutGroup>();
	}

	void Start()
	{
		GenerateGrid();
		AutoResizeCells();
		StartCoroutine(DestroyGridLayout());
	}

   IEnumerator DestroyGridLayout()
	{
		// Wait for end of frame to ensure layout is not in use
		yield return new WaitForEndOfFrame();
		if (gridLayout != null)
		{
			Destroy(gridLayout);
			gridLayout = null;
		}
	}
	void OnRectTransformDimensionsChange()
	{
		// Handle screen rotation or resolution change dynamically
		AutoResizeCells();
	}

	public void GenerateGrid()
	{
		if (cellPrefab == null)
		{
			Debug.LogError("Cell Prefab not assigned");
			return;
		}

		// Safely destroy existing children
		for (int i = transform.childCount - 1; i >= 0; i--)
		{
			Destroy(transform.GetChild(i).gameObject);
		}

		cells = new Cell[columns, rows];

		for (int x = 0; x < columns; x++)
		{
			for (int y = 0; y < rows; y++)
			{
				GameObject go = Instantiate(cellPrefab, transform);
				Cell c = go.GetComponent<Cell>();
				c.Initialize(new Vector2Int(x, y));
				cells[x, y] = c;

				Button btn = go.GetComponent<Button>();
				if (btn != null)
				{
					if (!isOpponentGrid)
					{
						// capture the coordinate value to avoid closure issues
						Vector2Int captured = c.coordinate;
                     btn.onClick.AddListener(() => OnCellClicked?.Invoke(captured));
					}
					else
					{
						btn.interactable = false;
					}
				}
			}
		}
	}

	private void AutoResizeCells()
	{
		if (gridLayout == null || gridArea == null) return;

		float gridWidth = gridArea.rect.width;
        float gridHeight = gridArea.rect.height;

		float cellSize = Mathf.Min(gridWidth / columns, gridHeight / rows);
		gridLayout.cellSize = new Vector2(cellSize, cellSize);
	}

	public Cell GetCell(Vector2Int coord)
	{
		if (coord.x < 0 || coord.x >= columns || coord.y < 0 || coord.y >= rows)
			return null;
        return cells[coord.x, coord.y];
	}

	/// <summary>
	/// Show a preview of where a ship would be placed starting at 'start' with given orientation.
	/// </summary>
	public void PreviewPlacement(Ship ship, Vector2Int start, bool isVertical)
	{
		ClearPreview();
		if (ship == null || ship.shipData == null) return;

		// store last preview state so we can reapply if orientation changes
		lastPreviewStart = start;
		hasPreviewStart = true;
		CurrentPreviewGrid = this;

		int len = ship.shipData.length;
		List<Cell> toPreview = new List<Cell>();
		for (int i = 0; i < len; i++)
		{
			Vector2Int coord = !isVertical ? new Vector2Int(start.x, start.y + i) : new Vector2Int(start.x + i, start.y);
			Cell c = GetCell(coord);
			if (c == null) { toPreview = null; break; }
			toPreview.Add(c);
		}

		if (toPreview == null) return;
		// mark preview visuals
		foreach (var c in toPreview)
		{
			c.SetPreview(true);
			previewCells.Add(c);
		}
	}

	public void ClearPreview()
	{
		foreach (var c in previewCells)
		{
			if (c != null) c.SetPreview(false);
		}
		previewCells.Clear();

		// clear stored preview start
		hasPreviewStart = false;
		CurrentPreviewGrid = null;
	}

	/// <summary>
	/// Reapply the preview for the stored start cell using the ship's current orientation.
	/// Useful when rotating the dragged ship without moving the cursor.
	/// </summary>
	public void UpdatePreviewForCurrentStart(Ship ship)
	{
		if (!hasPreviewStart || ship == null) return;
		PreviewPlacement(ship, lastPreviewStart, ship.isVertical);
	}

	/// <summary>
	/// Try to place a ship on the grid. Returns true if placed.
	/// </summary>
	public bool TryPlaceShip(Ship ship, Vector2Int start, bool isVertical)
	{
		if (ship == null || ship.shipData == null) return false;
		int len = ship.shipData.length;
		List<Cell> toPlace = new List<Cell>();
		for (int i = 0; i < len; i++)
		{
			Vector2Int coord = !isVertical ? new Vector2Int(start.x, start.y + i) : new Vector2Int(start.x + i, start.y);
			Cell c = GetCell(coord);
			if (c == null) return false; // out of bounds
			if (c.State != CellState.Empty) return false; // collision
			toPlace.Add(c);
		}

		// All checks passed: place ship
		foreach (var c in toPlace)
		{
			c.SetShip();
		}

		// Anchor and snap the ship GameObject to the grid visually.
		RectTransform shipRect = ship.GetComponent<RectTransform>();
		Cell firstCell = toPlace.Count > 0 ? toPlace[0] : null;
		Cell lastCell = toPlace.Count > 0 ? toPlace[toPlace.Count - 1] : null;
		if (shipRect != null && firstCell != null && lastCell != null)
		{
			RectTransform firstRect = firstCell.GetComponent<RectTransform>();
			RectTransform lastRect = lastCell.GetComponent<RectTransform>();
			// Parent the ship to the grid (same parent as cells) so anchoredPosition lines up
			shipRect.SetParent(this.transform, false);

			// Resize ship to cover the correct number of cells
			float cellW = firstRect.rect.width;
			float cellH = firstRect.rect.height;
			if (!isVertical)
			{
				shipRect.sizeDelta = new Vector2(len * cellW, cellH);
			}
			else
			{
				shipRect.sizeDelta = new Vector2(cellW, len * cellH);
			}

			// center ship between first and last cell
			Vector2 center = (firstRect.anchoredPosition + lastRect.anchoredPosition) * 0.5f;
			shipRect.pivot = new Vector2(0.5f, 0.5f);
			shipRect.anchoredPosition = center;

			// set rotation according to orientation
			ship.transform.rotation = Quaternion.Euler(0, 0, ship.isVertical ? 90f : 0f);
		}

		// Let the ship mark itself as placed and remember occupied cells
		ship.PlaceOnGrid(toPlace.ToArray());

		ClearPreview();
		return true;
	}
}
