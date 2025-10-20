using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;


public enum CellState { Empty, Ship, Miss, Hit }


public class Cell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDropHandler
{
	public Vector2Int coordinate;
	public Image background;
	public Image icon; // put hit/miss sprite here


	private CellState state = CellState.Empty;


	public CellState State => state;

	// preview visual flag
	private bool isPreview = false;


	public void Initialize(Vector2Int coord)
	{
		coordinate = coord;
		state = CellState.Empty;
		UpdateVisual();
	}


	public void SetShip()
	{
		state = CellState.Ship;
		UpdateVisual();
	}

	public void ClearShip()
	{
		if (state == CellState.Ship)
		{
			state = CellState.Empty;
			UpdateVisual();
		}
	}


	public void MarkMiss()
	{
		if (state == CellState.Empty)
		{
			state = CellState.Miss;
			UpdateVisual();
		}
	}


	public void MarkHit()
	{
		state = CellState.Hit;
		UpdateVisual();
	}

	// Preview helpers called by GridController when dragging
	public void SetPreview(bool preview)
	{
		isPreview = preview;
		UpdateVisual();
	}

	// Drop handling
	public void OnDrop(PointerEventData eventData)
	{
		var dragging = Ship.DraggingShip;
		if (dragging == null) return;

		// Ask GridController to try placing the ship starting at this cell
		var grid = GetComponentInParent<GridController>();
		if (grid != null)
		{
			grid.TryPlaceShip(dragging, coordinate, dragging.isVertical);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		var dragging = Ship.DraggingShip;
		if (dragging == null) return;

		var grid = GetComponentInParent<GridController>();
		if (grid != null)
		{
			grid.PreviewPlacement(dragging, coordinate, dragging.isVertical);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		var dragging = Ship.DraggingShip;
		if (dragging == null) return;

		var grid = GetComponentInParent<GridController>();
		if (grid != null)
		{
			grid.ClearPreview();
		}
	}


	private void UpdateVisual()
	{
		// Update background / icon based on state and preview
		if (isPreview)
		{
			if (background != null)
				background.color = Color.cyan * 0.8f;
			return;
		}

		if (background != null)
			background.color = Color.white;

		switch (state)
		{
			case CellState.Empty:
				if (icon != null) icon.enabled = false;
				break;
			case CellState.Ship:
				// For player's grid show ship indicator; for enemy hide it (controlled externally)
				if (icon != null) icon.enabled = true;
				break;
			case CellState.Miss:
				if (icon != null) icon.enabled = true;
				// set miss sprite
				break;
			case CellState.Hit:
				if (icon != null) icon.enabled = true;
				// set hit sprite
				break;
		}
	}
}