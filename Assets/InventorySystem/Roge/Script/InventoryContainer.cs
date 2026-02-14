using System.Collections.Generic;

public enum ContainerType
{
    Bag,
    Pocket,
    Warehouse,
    Trash
}

[System.Serializable]
public class InventoryContainer
{
    public string containerId;
    public ContainerType type;
    public List<InventorySlot> slots;

    public InventoryContainer(string id, ContainerType type, int size)
    {
        this.containerId = id;
        this.type = type;
        slots = new List<InventorySlot>();
        for (int i = 0; i < size; i++)
            slots.Add(new InventorySlot());
    }
}
