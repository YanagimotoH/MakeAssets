using UnityEngine;
using System.Collections.Generic;

public class ItemDataBase : MonoBehaviour
{
    public static ItemDataBase Instance;

    public List<ItemData> items;
    private Dictionary<int, ItemData> itemDict;

    void Awake()
    {
        Instance = this;
        itemDict = new Dictionary<int, ItemData>();
        foreach (var item in items)
            itemDict[item.itemId] = item;
    }

    public ItemData GetItem(int id)
    {
        return itemDict.TryGetValue(id, out var item) ? item : null;
    }
}
