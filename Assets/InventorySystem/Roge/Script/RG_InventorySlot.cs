[System.Serializable]
public class RG_InventorySlot
{
    public RG_RogeItem item;
    public int amount;

    public bool IsEmpty => item == null || amount <= 0;

    public void Clear()
    {
        item = null;
        amount = 0;
    }
}
