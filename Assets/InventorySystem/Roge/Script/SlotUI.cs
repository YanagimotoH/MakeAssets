using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if TMP_PRESENT
using TMPro;
#endif

/// <summary>
/// インベントリ用スロットUI（SC_SlotTemplate準拠）
/// </summary>
public class SlotUI : MonoBehaviour, IPointerClickHandler
{
    // ===== 識別 =====
    [HideInInspector] public string containerId;
    [HideInInspector] public int slotIndex;

    // ===== UI参照（Inspector設定）=====
    [Header("UI References")]
    public Image container;   // スロット背景
    public Image item;        // アイテムアイコン

#if TMP_PRESENT
    public TMP_Text count;
#else
    public Text count;
#endif

    // =========================
    // 初期化（InventoryUIから呼ぶ）
    // =========================
    public void Setup(string containerId, int index)
    {
        this.containerId = containerId;
        this.slotIndex = index;
    }

    // =========================
    // 表示更新
    // =========================
    public void Refresh(InventorySlot slot)
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
            return;
        }

        item.enabled = true;
        item.sprite = slot.item.icon;
        count.text = slot.amount.ToString();
    }

    // =========================
    // クリック処理
    // =========================
    public void OnPointerClick(PointerEventData eventData)
    {
        InventoryManager.Instance
            .SelectSlot(containerId, slotIndex);
    }
}
