using System.Collections.Generic;
using UnityEngine;

namespace SurvivalInventory
{
    public class ItemDataBase : MonoBehaviour
    {
        public static ItemDataBase Instance;

        public List<ItemData> items = new List<ItemData>();
        Dictionary<int, ItemData> itemDict;

        void Awake()
        {
            Instance = this;
            itemDict = new Dictionary<int, ItemData>();
            foreach (var item in items)
            {
                if (item != null)
                {
                    itemDict[item.itemId] = item;
                }
            }
        }

        public ItemData GetItem(int id)
        {
            return itemDict != null && itemDict.TryGetValue(id, out var item) ? item : null;
        }
    }
}
