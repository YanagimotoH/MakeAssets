using System.Collections.Generic;

public enum RG_ContainerType
{
    Bag,
    Pocket,
    Warehouse,
    Trash
}

[System.Serializable]
public class RG_InventoryContainer
{
    public string containerId;
    public RG_ContainerType type;
    public List<RG_InventorySlot> slots;

    public RG_InventoryContainer(string id, RG_ContainerType type, int size)
    {
        this.containerId = id;
        this.type = type;
        slots = new List<RG_InventorySlot>();
        for (int i = 0; i < size; i++)
            slots.Add(new RG_InventorySlot());
    }
}
