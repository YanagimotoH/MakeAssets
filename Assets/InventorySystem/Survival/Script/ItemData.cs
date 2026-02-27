using UnityEngine;

namespace SurvivalInventory
{
    [CreateAssetMenu(menuName = "Inventory/Survival Item")]
    public class ItemData : ScriptableObject
    {
        public int itemId;
        public string itemName;
        public Sprite icon;
        [TextArea]
        public string description;
        public float weight = 1f;
        public int maxStack = 99;
    }
}
