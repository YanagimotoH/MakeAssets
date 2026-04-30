using UnityEngine;
using System.Collections.Generic;

public class RG_ItemDataBase : MonoBehaviour
{
    public static RG_ItemDataBase Instance;

    public List<RG_RogeItem> items;
    private Dictionary<int, RG_RogeItem> itemDict;

    void Awake()
    {
        Instance = this;
        itemDict = new Dictionary<int, RG_RogeItem>();
        foreach (var item in items)
            itemDict[item.itemId] = item;
    }

    public RG_RogeItem GetItem(int id)
    {
        return itemDict.TryGetValue(id, out var item) ? item : null;
    }
}
