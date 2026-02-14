using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class InventorySaveSystem : MonoBehaviour
{
    string path => Application.persistentDataPath + "/inventory.json";

    public void Save()
    {
        var data = new InventorySaveData
        {
            containers = new List<ContainerSaveData>()
        };

        foreach (var c in InventoryManager.Instance.containers.Values)
        {
            var containerData = new ContainerSaveData
            {
                containerId = c.containerId,
                slots = new List<SlotSaveData>()
            };

            foreach (var slot in c.slots)
            {
                containerData.slots.Add(new SlotSaveData
                {
                    itemId = slot.IsEmpty ? -1 : slot.item.itemId,
                    amount = slot.amount
                });
            }

            data.containers.Add(containerData);
        }

        File.WriteAllText(path, JsonUtility.ToJson(data, true));
    }

    public void Load()
    {
        if (!File.Exists(path)) return;

        var json = File.ReadAllText(path);
        var data = JsonUtility.FromJson<InventorySaveData>(json);

        foreach (var cData in data.containers)
        {
            var container = InventoryManager.Instance.containers[cData.containerId];

            for (int i = 0; i < container.slots.Count; i++)
            {
                var slot = container.slots[i];
                var sData = cData.slots[i];

                if (sData.itemId < 0)
                {
                    slot.Clear();
                }
                else
                {
                    slot.item = ItemDataBase.Instance.GetItem(sData.itemId);
                    slot.amount = sData.amount;
                }
            }
        }
    }
}
