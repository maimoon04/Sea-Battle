using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CannonController : MonoBehaviour
{
    [Header("References")]
    public GridController targetGrid; // The grid to fire at
    public GameObject cannonballPrefab;
    public Transform parent; // Optional: Visual cannonball prefab
    public AudioSource fireSound; // Optional: Sound effect

    [Header("Game Logic")]
    public TurnManager turnManager;
    public ShipSpawner shipSpawner;
    public void FireAtCell(Vector2Int coordinate)
    {

        Cell targetCell = targetGrid.GetCell(coordinate);
        if (targetCell == null) return;

        // Only allow firing at cells that haven't been hit or missed yet
        if (targetCell.State == CellState.Empty || targetCell.State == CellState.Ship)
        {
            // Play fire sound if available
            if (fireSound != null)
                fireSound.Play();

            // Spawn visual cannonball if prefab is set
            if (cannonballPrefab != null)
            {
                GameObject cannonball = Instantiate(cannonballPrefab, parent);
                // You can add animation here for the cannonball
            }

            // Check hit or miss
            if (targetCell.State == CellState.Ship)
            {
                targetCell.MarkHit();

                // Check if this cell belongs to a ship and if that ship is sunk
                List<Ship> ships = shipSpawner.spawnedShips;
                foreach (var ship in ships)
                {
                    if (ship.OccupiedCells != null)
                    {
                        foreach (var c in ship.OccupiedCells)
                        {
                            if (c == targetCell)
                            {
                                // This ship was hit
                                if (ship.IsSunk() && turnManager != null)
                                {
                                    turnManager.OnShipSunk(targetGrid, ship);
                                }
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                targetCell.MarkMiss();
            }

        
        }
    }
}