using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public Dictionary<string, InventoryContainer> containers
        = new Dictionary<string, InventoryContainer>();

    [System.Serializable]
    public class ContainerSetting
    {
        public string containerId;
        public ContainerType type;
        public int slotCount = 8;
    }

    [Header("▼ インベントリコンテナ設定（Inspectorで編集）")]
    [SerializeField]
    private List<ContainerSetting> containerSettings = new List<ContainerSetting>
    {
        new ContainerSetting { containerId = "Pocket_1", type = ContainerType.Pocket, slotCount = 8 },
        new ContainerSetting { containerId = "Pocket_2", type = ContainerType.Pocket, slotCount = 8 },
        new ContainerSetting { containerId = "Bag_01", type = ContainerType.Bag, slotCount = 20 },
        new ContainerSetting { containerId = "Warehouse", type = ContainerType.Warehouse, slotCount = 40 },
        new ContainerSetting { containerId = "Trash", type = ContainerType.Trash, slotCount = 1 }
    };

    // ===== 選択中スロット =====
    string selectedContainerId;
    int selectedSlotIndex = -1;

    void Awake()
    {
        Instance = this;

        // Inspectorで設定した内容でコンテナを生成
        foreach (var setting in containerSettings)
        {
            Register(new InventoryContainer(setting.containerId, setting.type, setting.slotCount));
        }
    }

    public void Register(InventoryContainer container)
    {
        containers[container.containerId] = container;
    }

    // ===== スロット選択 =====
    public void SelectSlot(string containerId, int slotIndex)
    {
        selectedContainerId = containerId;
        selectedSlotIndex = slotIndex;
    }

    public bool HasSelection()
    {
        return !string.IsNullOrEmpty(selectedContainerId)
               && selectedSlotIndex >= 0;
    }

    public void ClearSelection()
    {
        selectedContainerId = null;
        selectedSlotIndex = -1;
    }

    // ===== ★送信API（UIはこれだけ呼ぶ）=====
    public void SendSelectedTo(string targetContainerId)
    {
        if (!HasSelection()) return;

        MoveItem(
            selectedContainerId,
            targetContainerId,
            selectedSlotIndex
        );

        ClearSelection();
    }

    // ===== 内部処理 =====
    void MoveItem(string fromId, string toId, int slotIndex)
    {
        var from = containers[fromId];
        var to = containers[toId];
        var slot = from.slots[slotIndex];

        if (slot.IsEmpty) return;

        // ゴミ箱
        if (to.type == ContainerType.Trash)
        {
            slot.Clear();
            return;
        }

        AddItem(toId, slot.item, slot.amount);
        slot.Clear();
    }

    public void AddItem(string containerId, ItemData item, int amount)
    {
        var container = containers[containerId];

        // スタック
        foreach (var slot in container.slots)
        {
            if (!slot.IsEmpty && slot.item == item && slot.amount < item.maxStack)
            {
                int add = Mathf.Min(amount, item.maxStack - slot.amount);
                slot.amount += add;
                amount -= add;
                if (amount <= 0) return;
            }
        }

        // 空きスロット
        foreach (var slot in container.slots)
        {
            if (slot.IsEmpty)
            {
                slot.item = item;
                slot.amount = amount;
                return;
            }
        }
    }
}
