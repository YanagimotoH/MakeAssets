using UnityEngine;

public enum RG_RogeItemType
{
    Drink,
    Weapon
}

[CreateAssetMenu(menuName = "Inventory/RogeItem")]
public class RG_RogeItem : ScriptableObject
{
    public int itemId;
    public string itemName;
    public Sprite icon;
    public int maxStack = 99;
    public RG_RogeItemType itemType;
}
