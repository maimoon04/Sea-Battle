using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShipSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject shipPrefab; // prefab containing Ship component
    public RectTransform container; // UI container to parent spawned ships
    public List<ShipData> shipsToSpawn = new List<ShipData>();
    [HideInInspector]
    public List<Ship> spawnedShips = new List<Ship>();

    [Header("Layout")]
    public float cellSize = 10f;
    public float spacing = 5f;

    // Clear existing spawned ships in container
    public void Clear()
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(container.GetChild(i).gameObject);
        }
        spawnedShips.Clear();
    }

    // Spawn all ships from the configured ShipData list
    [ContextMenu("Spawn All Ships")]
    public void SpawnAll()
    {
        if (shipPrefab == null || container == null) return;

       

        float x = 0f;
        foreach (var sd in shipsToSpawn)
        {
            GameObject go = Instantiate(shipPrefab, container);
            go.transform.localScale = Vector3.one;
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.pivot = new Vector2(0, 1);
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(x, 0);
            }

            Ship ship = go.GetComponent<Ship>();
            if (ship != null)
            {
                ship.Initialize(sd, cellSize, spacing);
                spawnedShips.Add(ship);
            }

            // Advance x by ship width + spacing
            float width = (sd != null) ? (sd.length * cellSize + (sd.length - 1) * spacing) : cellSize;
            x += width + spacing;
        }
    }

    public void TurnOffAllShips()
    {
        foreach (var ship in spawnedShips)
        {
            if (ship != null)
            {
                ship.image.raycastTarget = false;
            }
        }
    }
    // Returns true when all spawned ships have been placed on the grid
    public bool AllShipsPlaced()
    {
        if (spawnedShips == null || spawnedShips.Count == 0) return false;
        foreach (var s in spawnedShips)
        {
            if (s == null || !s.isPlaced) return false;
        }
        return true;
    }
}
