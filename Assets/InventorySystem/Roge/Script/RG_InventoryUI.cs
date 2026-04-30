using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RG_InventoryUI : MonoBehaviour
{
    [System.Serializable]
    public class ContainerView
    {
        public string containerId;
        public Transform slotRoot;   // GridLayoutGroup 等
    }

    [Header("Containers")]
    public List<ContainerView> containerViews;

    [Header("Slot UI")]
    public GameObject slotPrefab;   // ★ SlotUIが付いたPrefab

    [Header("Action Panel")]
    public RectTransform actionPanel;
    public Vector2 actionPanelOffset = new Vector2(120f, 0f);
    public Button useButton;
    public Button equipButton;
    public Button warehouseButton;
    public Button trashButton;
    public Button bagButton;
    public Button pocket1Button;
    public Button pocket2Button;

    [Header("Action Container Ids")]
    public string warehouseContainerId = "Warehouse";
    public string trashContainerId = "Trash";
    public string bagContainerId = "Bag_01";
    public string pocket1ContainerId = "Pocket_1";
    public string pocket2ContainerId = "Pocket_2";

    Dictionary<string, List<RG_SlotUI>> slotUIs
        = new Dictionary<string, List<RG_SlotUI>>();

    string selectedContainerId;
    int selectedSlotIndex = -1;
    RG_RogeItem selectedItem;

    void Start()
    {
        BuildAll();
        RefreshAll();
        UpdateActionPanelVisibility(false);
    }

    // =========================
    // UI構築
    // =========================
    void BuildAll()
    {
        foreach (var view in containerViews)
        {
            var container =
                RG_InventoryManager.Instance.containers[view.containerId];

            var list = new List<RG_SlotUI>();
            slotUIs[view.containerId] = list;

            for (int i = 0; i < container.slots.Count; i++)
            {
                GameObject go =
                    Instantiate(slotPrefab, view.slotRoot);

                // ★ 追加しない。取得する
                var slotUI = go.GetComponent<RG_SlotUI>();
                if (slotUI == null)
                {
                    Debug.LogError("SlotPrefabにSlotUIが付いていません", go);
                    continue;
                }

                slotUI.Setup(view.containerId, i, this);
                list.Add(slotUI);
            }
        }
    }

    // =========================
    // UI更新
    // =========================
    public void RefreshAll()
    {
        foreach (var view in containerViews)
        {
            RefreshContainer(view.containerId);
        }

        RefreshActionPanel();
    }

    void RefreshContainer(string containerId)
    {
        var container =
            RG_InventoryManager.Instance.containers[containerId];

        var uiList = slotUIs[containerId];

        for (int i = 0; i < uiList.Count; i++)
        {
            uiList[i].Refresh(container.slots[i]);
        }
    }

    // =========================
    // ボタン用（Inspector登録）
    // =========================
    public void SendTo(string targetContainerId)
    {
        RG_InventoryManager.Instance
            .SendSelectedTo(targetContainerId);
        ClearSelection();
        RefreshAll();
    }

    public void HandleSlotSelected(string containerId, int slotIndex, RectTransform slotTransform)
    {
        if (selectedContainerId == containerId && selectedSlotIndex == slotIndex && actionPanel != null && actionPanel.gameObject.activeSelf)
        {
            ClearSelection();
            return;
        }

        selectedContainerId = containerId;
        selectedSlotIndex = slotIndex;

        var container = RG_InventoryManager.Instance.containers[containerId];
        var slot = container.slots[slotIndex];

        if (slot == null || slot.IsEmpty)
        {
            ClearSelection();
            return;
        }

        selectedItem = slot.item;
        UpdateActionPanelVisibility(true);
        UpdateActionPanelPosition(slotTransform);
        UpdateActionButtons(selectedItem, selectedContainerId);
    }

    public void SendSelectedToWarehouse()
    {
        SendTo(warehouseContainerId);
    }

    public void SendSelectedToTrash()
    {
        SendTo(trashContainerId);
    }

    public void SendSelectedToBag()
    {
        SendTo(bagContainerId);
    }

    public void SendSelectedToPocket1()
    {
        SendTo(pocket1ContainerId);
    }

    public void SendSelectedToPocket2()
    {
        SendTo(pocket2ContainerId);
    }

    public void UseSelectedItem()
    {
        RefreshAll();
    }

    public void EquipSelectedItem()
    {
        RefreshAll();
    }

    void RefreshActionPanel()
    {
        if (string.IsNullOrEmpty(selectedContainerId) || selectedSlotIndex < 0)
        {
            UpdateActionPanelVisibility(false);
            return;
        }

        if (!RG_InventoryManager.Instance.containers.TryGetValue(selectedContainerId, out var container))
        {
            UpdateActionPanelVisibility(false);
            return;
        }

        if (selectedSlotIndex >= container.slots.Count)
        {
            UpdateActionPanelVisibility(false);
            return;
        }

        var slot = container.slots[selectedSlotIndex];
        if (slot == null || slot.IsEmpty)
        {
            ClearSelection();
            return;
        }

        selectedItem = slot.item;
        UpdateActionButtons(selectedItem, selectedContainerId);
    }

    void UpdateActionButtons(RG_RogeItem item, string currentContainerId)
    {
        if (item == null)
        {
            UpdateActionPanelVisibility(false);
            return;
        }

        SetButtonActive(useButton, item.itemType == RG_RogeItemType.Drink);
        SetButtonActive(equipButton, item.itemType == RG_RogeItemType.Weapon);
        SetButtonActive(warehouseButton, currentContainerId != warehouseContainerId);
        SetButtonActive(trashButton, currentContainerId != trashContainerId);
        SetButtonActive(bagButton, currentContainerId != bagContainerId);
        SetButtonActive(pocket1Button, currentContainerId != pocket1ContainerId);
        SetButtonActive(pocket2Button, currentContainerId != pocket2ContainerId);
    }

    void UpdateActionPanelPosition(RectTransform slotTransform)
    {
        if (actionPanel == null || slotTransform == null)
        {
            return;
        }

        var parentRect = actionPanel.parent as RectTransform;
        if (parentRect == null)
        {
            actionPanel.position = slotTransform.position + (Vector3)actionPanelOffset;
            return;
        }

        var canvas = actionPanel.GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, slotTransform.position);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, camera, out var localPoint))
        {
            actionPanel.position = slotTransform.position + (Vector3)actionPanelOffset;
            return;
        }

        var targetPosition = localPoint + actionPanelOffset;
        var panelSize = actionPanel.rect.size;
        var min = parentRect.rect.min + Vector2.Scale(panelSize, actionPanel.pivot);
        var max = parentRect.rect.max - Vector2.Scale(panelSize, Vector2.one - actionPanel.pivot);

        targetPosition.x = Mathf.Clamp(targetPosition.x, min.x, max.x);
        targetPosition.y = Mathf.Clamp(targetPosition.y, min.y, max.y);

        actionPanel.anchoredPosition = targetPosition;
    }

    void UpdateActionPanelVisibility(bool visible)
    {
        if (actionPanel != null)
        {
            actionPanel.gameObject.SetActive(visible);
        }
    }

    void ClearSelection()
    {
        selectedContainerId = null;
        selectedSlotIndex = -1;
        selectedItem = null;
        UpdateActionPanelVisibility(false);
        RG_InventoryManager.Instance.ClearSelection();
    }

    void SetButtonActive(Button button, bool active)
    {
        if (button != null)
        {
            button.gameObject.SetActive(active);
        }
    }
}
