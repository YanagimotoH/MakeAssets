using UnityEngine;
using System.Collections.Generic;

public class RG_InventoryManager : MonoBehaviour
{
    public static RG_InventoryManager Instance;

    public Dictionary<string, RG_InventoryContainer> containers
        = new Dictionary<string, RG_InventoryContainer>();

    [System.Serializable]
    public class ContainerSetting
    {
        public string containerId;
        public RG_ContainerType type;
        public int slotCount = 8;
    }

    [Header("▼ インベントリコンテナ設定（Inspectorで編集）")]
    [SerializeField]
    private List<ContainerSetting> containerSettings = new List<ContainerSetting>
    {
        new ContainerSetting { containerId = "Pocket_1", type = RG_ContainerType.Pocket, slotCount = 8 },
        new ContainerSetting { containerId = "Pocket_2", type = RG_ContainerType.Pocket, slotCount = 8 },
        new ContainerSetting { containerId = "Bag_01", type = RG_ContainerType.Bag, slotCount = 20 },
        new ContainerSetting { containerId = "Warehouse", type = RG_ContainerType.Warehouse, slotCount = 40 },
        new ContainerSetting { containerId = "Trash", type = RG_ContainerType.Trash, slotCount = 1 }
    };

    [System.Serializable]
    public class InitialItemSetting
    {
        public string containerId;
        public RG_RogeItem item;
        public int amount = 1;
    }

    [Header("▼ 初期アイテム設定（Inspectorで編集）")]
    [SerializeField]
    private List<InitialItemSetting> initialItems = new List<InitialItemSetting>();

    // ===== 選択中スロット =====
    string selectedContainerId;
    int selectedSlotIndex = -1;

    void Awake()
    {
        Instance = this;

        // Inspectorで設定した内容でコンテナを生成
        foreach (var setting in containerSettings)
        {
            Register(new RG_InventoryContainer(setting.containerId, setting.type, setting.slotCount));
        }

        ApplyInitialItems();
    }

    public void Register(RG_InventoryContainer container)
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
        if (to.type == RG_ContainerType.Trash)
        {
            var trashSlot = to.slots.Count > 0 ? to.slots[0] : null;
            if (trashSlot != null)
            {
                trashSlot.item = slot.item;
                trashSlot.amount = slot.amount;
            }
            slot.Clear();
            return;
        }

        if (TryAddItemOrSwap(to, slot.item, slot.amount, out var swappedItem, out var swappedAmount))
        {
            slot.item = swappedItem;
            slot.amount = swappedAmount;
            return;
        }

        slot.Clear();
    }

    public void AddItem(string containerId, RG_RogeItem item, int amount)
    {
        if (item == null || amount <= 0 || string.IsNullOrEmpty(containerId))
        {
            return;
        }

        if (!containers.TryGetValue(containerId, out var container))
        {
            return;
        }

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

    bool TryAddItemOrSwap(RG_InventoryContainer container, RG_RogeItem item, int amount, out RG_RogeItem swappedItem, out int swappedAmount)
    {
        swappedItem = null;
        swappedAmount = 0;

        if (item == null || amount <= 0)
        {
            return false;
        }

        // スタック
        foreach (var slot in container.slots)
        {
            if (!slot.IsEmpty && slot.item == item && slot.amount < item.maxStack)
            {
                int add = Mathf.Min(amount, item.maxStack - slot.amount);
                slot.amount += add;
                amount -= add;
                if (amount <= 0)
                {
                    return false;
                }
            }
        }

        // 空きスロット
        foreach (var slot in container.slots)
        {
            if (slot.IsEmpty)
            {
                slot.item = item;
                slot.amount = amount;
                return false;
            }
        }

        // 交換
        var swapSlot = container.slots.Count > 0 ? container.slots[0] : null;
        if (swapSlot == null)
        {
            return false;
        }

        swappedItem = swapSlot.item;
        swappedAmount = swapSlot.amount;
        swapSlot.item = item;
        swapSlot.amount = amount;
        return true;
    }

    void ApplyInitialItems()
    {
        foreach (var setting in initialItems)
        {
            AddItem(setting.containerId, setting.item, setting.amount);
        }
    }
}
