using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public int itemId;
    public string itemName;
    public Sprite icon;
    public int maxStack = 99;
}
