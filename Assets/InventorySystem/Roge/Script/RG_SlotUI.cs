using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if TMP_PRESENT
using TMPro;
#endif

/// <summary>
/// インベントリ用スロットUI（SC_SlotTemplate準拠）
/// </summary>
public class RG_SlotUI : MonoBehaviour, IPointerClickHandler
{
    // ===== 識別 =====
    [HideInInspector] public string containerId;
    [HideInInspector] public int slotIndex;
    [HideInInspector] public RG_InventoryUI inventoryUI;

    // ===== UI参照（Inspector設定）=====
    [Header("UI References")]
    public Image container;   // スロット背景
    public Image item;        // アイテムアイコン

#if TMP_PRESENT
    public TMP_Text count;
    public TMP_Text itemName;
#else
    public Text count;
    public Text itemName;
#endif

    // =========================
    // 初期化（InventoryUIから呼ぶ）
    // =========================
    public void Setup(string containerId, int index, RG_InventoryUI inventoryUI)
    {
        this.containerId = containerId;
        this.slotIndex = index;
        this.inventoryUI = inventoryUI;
    }

    // =========================
    // 表示更新
    // =========================
    public void Refresh(RG_InventorySlot slot)
    {
        if (item == null || count == null)
        {
            Debug.LogError("SlotUI: UI references not set.", this);
            return;
        }

        if (slot == null || slot.IsEmpty)
        {
            item.enabled = false;
            count.text = "";
            if (itemName != null)
            {
                itemName.text = "";
            }
            return;
        }

        item.enabled = true;
        item.sprite = slot.item.icon;
        count.text = slot.amount.ToString();
        if (itemName != null)
        {
            itemName.text = slot.item.itemName;
        }
    }

    // =========================
    // クリック処理
    // =========================
    public void OnPointerClick(PointerEventData eventData)
    {
        RG_InventoryManager.Instance
            .SelectSlot(containerId, slotIndex);

        if (inventoryUI != null)
        {
            inventoryUI.HandleSlotSelected(containerId, slotIndex, transform as RectTransform);
        }
    }
}
