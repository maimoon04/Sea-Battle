using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShipSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject shipPrefab; // prefab containing Ship component
    public RectTransform container; // UI container to parent spawned ships
    public List<ShipData> shipsToSpawn = new List<ShipData>();

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
            }

            // Advance x by ship width + spacing
            float width = (sd != null) ? (sd.length * cellSize + (sd.length - 1) * spacing) : cellSize;
            x += width + spacing;
        }
    }
}
