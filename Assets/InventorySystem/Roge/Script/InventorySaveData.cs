using System.Collections.Generic;

[System.Serializable]
public class InventorySaveData
{
    public List<ContainerSaveData> containers;
}

[System.Serializable]
public class ContainerSaveData
{
    public string containerId;
    public List<SlotSaveData> slots;
}

[System.Serializable]
public class SlotSaveData
{
    public int itemId;
    public int amount;
}
