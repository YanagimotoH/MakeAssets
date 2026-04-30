using System.Collections.Generic;

[System.Serializable]
public class RG_InventorySaveData
{
    public List<RG_ContainerSaveData> containers;
}

[System.Serializable]
public class RG_ContainerSaveData
{
    public string containerId;
    public List<RG_SlotSaveData> slots;
}

[System.Serializable]
public class RG_SlotSaveData
{
    public int itemId;
    public int amount;
}
