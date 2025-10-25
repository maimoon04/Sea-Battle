using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CannonController : MonoBehaviour
{
    [Header("References")]
    public GridController targetGrid; // The grid to fire at
    public GameObject cannonballPrefab;
    public GameObject cannonballParent;
    public Transform cannonTransform; // The 2D cannon sprite transform
    public float cannonRotationSpeed = 2f; // Speed at which cannon rotates
    public float projectileSpeed = 3f; // Speed of the cannonball
    public AudioSource fireSound; // Optional: Sound effect
    [Header("Game Logic")]
    public TurnManager turnManager;
    public ShipSpawner shipSpawner;
    
    private bool shipSunk =false;
    private bool isActive = false; // Whether this cannon is currently active
    private void Update()
    {
        // Only process input when this cannon is active
        if (!isActive) return;
    }

   

    public void FireAtCell(Vector2Int coordinate,Action<bool> onFireComplete)
    {


        Cell targetCell = targetGrid.GetCell(coordinate);
    
       // if (targetCell == null) return;

        // Only allow firing at cells that haven't been hit or missed yet
        if (targetCell.State == CellState.Empty || targetCell.State == CellState.Ship)
        {
            // Get the target position in world space
            Vector3 targetPosition = targetCell.transform.position;
            
            // Calculate direction to target
            Vector3 direction = (targetPosition - cannonTransform.position).normalized;
            
            // Calculate angle to target
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // Rotate cannon to face target
            cannonTransform.rotation = Quaternion.Euler(0, 0, angle);

            // Play fire sound if available
            if (fireSound != null)
                fireSound.Play();

            // Spawn and setup cannonball
            
                GameObject cannonball = Instantiate(cannonballPrefab, cannonTransform);
                StartCoroutine(MoveCannonball(cannonball, targetPosition, targetCell,onFireComplete));
            
        }
    }

    private IEnumerator MoveCannonball(GameObject cannonball, Vector3 targetPosition, Cell targetCell, Action<bool> onFireComplete)
    {
        Transform cannonballTransform = cannonball.transform;
        Vector3 startPos = cannonballTransform.position;
        Vector3 moveDirection = targetPosition - startPos;
        float distance = moveDirection.magnitude;
        moveDirection /= distance; // normalize once instead of per-frame
        
        // Cache transform and scale values
        Vector3 startScale = cannonballTransform.localScale;
        Vector3 midScale = startScale * 0.6f;
        Vector3 endScale = startScale * 0.2f;
        
        float journeyTime = distance / projectileSpeed;
        float elapsedTime = 0;
        
        // Reusable vector to reduce garbage collection
        Vector3 currentPos = startPos;
        Vector3 currentScale = startScale;
        
        int frameSkip = QualitySettings.vSyncCount == 0 ? 0 : 1; // Skip frames on lower-end devices
        int frameCount = 0;

        while (elapsedTime < journeyTime)
        {
            if (frameCount++ % (frameSkip + 1) == 0)
            {
                float deltaTime = Time.deltaTime;
                elapsedTime += deltaTime;
                float t = elapsedTime / journeyTime;
                
                // More efficient position calculation
                currentPos.x = Mathf.LerpUnclamped(startPos.x, targetPosition.x, t);
                currentPos.y = Mathf.LerpUnclamped(startPos.y, targetPosition.y, t);
                currentPos.z = -Mathf.Sin(t * Mathf.PI) * 2f;
                
                // Efficient scale interpolation
                float scaleT = t;
                currentScale.x = startScale.x + (midScale.x - startScale.x) * scaleT;
                currentScale.y = startScale.y + (midScale.y - startScale.y) * scaleT;
                currentScale.z = startScale.z + (midScale.z - startScale.z) * scaleT;
                
                cannonballTransform.position = currentPos;
                cannonballTransform.localScale = currentScale;
            }
            yield return null;
        }
        
        // Optimized impact animation
        float impactTime = 0.1f;
        float impactElapsed = 0;
        frameCount = 0;
        
        while (impactElapsed < impactTime)
        {
            if (frameCount++ % (frameSkip + 1) == 0)
            {
                float deltaTime = Time.deltaTime;
                impactElapsed += deltaTime;
                float t = impactElapsed / impactTime;
                
                // Direct scale calculation instead of Lerp
                currentScale.x = midScale.x + (endScale.x - midScale.x) * t;
                currentScale.y = midScale.y + (endScale.y - midScale.y) * t;
                currentScale.z = midScale.z + (endScale.z - midScale.z) * t;
                
                cannonballTransform.localScale = currentScale;
            }
            yield return null;
        }

        // Destroy the cannonball when it reaches the target
        Destroy(cannonball);

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
                                shipSunk = true;
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
        onFireComplete?.Invoke(targetCell.State == CellState.Hit && !shipSunk);
        shipSunk = false;
    }
}