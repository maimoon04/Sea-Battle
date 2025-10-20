using UnityEngine;

[CreateAssetMenu(fileName = "ShipData", menuName = "Scriptable Objects/ShipData")]
public class ShipData : ScriptableObject
{
 public string shipName;
 public int length = 3;
 public Sprite shipSprite;
}
