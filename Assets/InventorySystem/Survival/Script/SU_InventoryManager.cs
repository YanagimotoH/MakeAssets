using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace SurvivalInventory
{
    public class SU_InventoryManager : MonoBehaviour
    {
        [System.Serializable]
        public class Slot
        {
            public int itemId = -1;
            public int count;
            [HideInInspector]
            public SU_SlotTemplate slot;
        }

        [System.Serializable]
        public class InitialItem
        {
            public ItemData item;
            public int count = 1;
        }

        [System.Serializable]
        public class InventoryGroup
        {
            public RectTransform slotsContainer;
            public SU_SlotTemplate slotTemplate;
            public int slotCount = 20;
            public int slotMargin = 5;
            public float maxWeight = 50f;
            public Text weightText;
            public System.Collections.Generic.List<InitialItem> initialItems = new System.Collections.Generic.List<InitialItem>();

            [HideInInspector]
            public Slot[] slots;
            [HideInInspector]
            public float currentWeight;
            [HideInInspector]
            public Color currentWeightDefaultColor;
        }

        [Header("Data")]
        [SerializeField]
        ItemDataBase itemDataBase;

        [Header("Item Info")]
        [SerializeField]
        Text selectedItemNameText;
        [SerializeField]
        Text selectedItemWeightText;
        [SerializeField]
        Text selectedItemDescriptionText;

        [Header("Groups")]
        [SerializeField]
        System.Collections.Generic.List<InventoryGroup> groups = new System.Collections.Generic.List<InventoryGroup>();

        [Header("Drag")]
        [SerializeField]
        Image dragIcon;
        [SerializeField, Range(0f, 1f)]
        float dragIconAlpha = 0.6f;

        SU_SlotTemplate selectedSlot;

        struct HeldState
        {
            public int itemId;
            public int count;
            public int sourceGroupIndex;
            public int sourceSlotIndex;
            public bool fromDrag;
            public bool dropHandled;
        }

        HeldState heldState;

        bool IsHolding => heldState.itemId != -1 && heldState.count > 0;

        void Start()
        {
            if (itemDataBase == null)
            {
                itemDataBase = FindFirstObjectByType<ItemDataBase>();
            }

            if (dragIcon != null)
            {
                dragIcon.enabled = false;
                dragIcon.raycastTarget = false;
            }

            if (groups == null || groups.Count == 0)
            {
                return;
            }

            for (int i = 0; i < groups.Count; i++)
            {
                InitializeSlots(groups[i], i);
                RecalculateWeight(groups[i]);
                UpdateSlotsUI(groups[i]);
                UpdateGroupWeightUI(groups[i]);

                if (groups[i].initialItems != null)
                {
                    foreach (var initial in groups[i].initialItems)
                    {
                        if (initial != null && initial.item != null && initial.count > 0)
                        {
                            TryAddItem(i, initial.item.itemId, initial.count);
                        }
                    }
                }
            }
        }

        void Update()
        {
            UpdateDragIcon();
        }

        void InitializeSlots(InventoryGroup group, int groupIndex)
        {
            if (group == null || group.slotsContainer == null || group.slotTemplate == null)
            {
                return;
            }

            group.currentWeightDefaultColor = group.weightText != null ? group.weightText.color : Color.white;

            group.slots = new Slot[group.slotCount];

            group.slotTemplate.container.rectTransform.pivot = new Vector2(0, 1);
            group.slotTemplate.container.rectTransform.anchorMax = group.slotTemplate.container.rectTransform.anchorMin = new Vector2(0, 1);
            group.slotTemplate.inventoryManager = this;
            group.slotTemplate.gameObject.SetActive(false);

            int resetIndex = 0;
            int rowTmp = 0;
            for (int i = 0; i < group.slots.Length; i++)
            {
                group.slots[i] = new Slot();
                GameObject newSlot = Instantiate(group.slotTemplate.gameObject, group.slotsContainer.transform);
                group.slots[i].slot = newSlot.GetComponent<SU_SlotTemplate>();
                group.slots[i].slot.inventoryManager = this;
                group.slots[i].slot.groupIndex = groupIndex;
                group.slots[i].slot.slotIndex = i;
                group.slots[i].slot.gameObject.SetActive(true);

                float xTmp = (int)((group.slotMargin + group.slots[i].slot.container.rectTransform.sizeDelta.x) * (i - resetIndex));
                if (xTmp + group.slots[i].slot.container.rectTransform.sizeDelta.x + group.slotMargin > group.slotsContainer.rect.width)
                {
                    resetIndex = i;
                    rowTmp++;
                    xTmp = 0;
                }

                group.slots[i].slot.container.rectTransform.anchoredPosition = new Vector2(group.slotMargin + xTmp, -group.slotMargin - ((group.slotMargin + group.slots[i].slot.container.rectTransform.sizeDelta.y) * rowTmp));
            }

            group.slotTemplate.container.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            group.slotTemplate.item.raycastTarget = group.slotTemplate.count.raycastTarget = false;
        }

        public void HandleSlotClick(SU_SlotTemplate slotTemplate, PointerEventData.InputButton button)
        {
            if (slotTemplate == null)
            {
                return;
            }

            InventoryGroup group = GetGroup(slotTemplate.groupIndex);
            if (group == null)
            {
                return;
            }

            if (button == PointerEventData.InputButton.Left)
            {
                SetSelectedSlot(slotTemplate);
                Slot clickedSlot = group.slots[slotTemplate.slotIndex];
                ItemData item = clickedSlot.itemId != -1 ? itemDataBase.GetItem(clickedSlot.itemId) : null;
                UpdateSelectedItemUI(item);
                return;
            }

            if (button == PointerEventData.InputButton.Right)
            {
                SetSelectedSlot(slotTemplate);
                if (IsHolding)
                {
                    if (TryPlaceHeldOnSlot(slotTemplate, 1, false))
                    {
                        UpdateGroupWeightUI(group);
                    }
                }
                else
                {
                    if (PickUpFromSlot(slotTemplate, 1, false))
                    {
                        UpdateGroupWeightUI(group);
                    }
                }
            }
        }

        void SetSelectedSlot(SU_SlotTemplate slotTemplate)
        {
            if (selectedSlot == slotTemplate)
            {
                return;
            }

            if (selectedSlot != null)
            {
                selectedSlot.SetSelected(false);
            }

            selectedSlot = slotTemplate;

            if (selectedSlot != null)
            {
                selectedSlot.SetSelected(true);
            }
        }

        public bool TryAddItem(int groupIndex, int itemId, int count)
        {
            InventoryGroup group = GetGroup(groupIndex);
            if (group == null || count <= 0 || itemDataBase == null)
            {
                return false;
            }

            ItemData item = itemDataBase.GetItem(itemId);
            if (item == null)
            {
                return false;
            }

            int capacity = CalculateAvailableCapacity(group, itemId, item.maxStack);
            if (capacity < count)
            {
                return false;
            }

            float totalWeight = group.currentWeight + (item.weight * count);
            if (totalWeight > group.maxWeight)
            {
                return false;
            }

            int added = AddToGroupInternal(group, item, count);
            UpdateSlotsUI(group);
            UpdateGroupWeightUI(group);
            return added == count;
        }

        public void BeginDrag(SU_SlotTemplate slotTemplate)
        {
            if (slotTemplate == null || IsHolding)
            {
                return;
            }

            SetSelectedSlot(slotTemplate);
            PickUpFromSlot(slotTemplate, int.MaxValue, true);
        }

        public void HandleDrop(SU_SlotTemplate targetSlotTemplate)
        {
            if (targetSlotTemplate == null || !IsHolding)
            {
                return;
            }

            bool placed = TryPlaceHeldOnSlot(targetSlotTemplate, heldState.count, true);
            if (!placed)
            {
                return;
            }

            heldState.dropHandled = true;

            if (IsHolding && heldState.fromDrag)
            {
                ReturnHeldToSource();
            }
        }

        public void EndDrag()
        {
            if (!IsHolding || !heldState.fromDrag)
            {
                return;
            }

            if (!heldState.dropHandled)
            {
                ReturnHeldToSource();
            }
        }

        InventoryGroup GetGroup(int groupIndex)
        {
            if (groups == null || groupIndex < 0 || groupIndex >= groups.Count)
            {
                return null;
            }

            return groups[groupIndex];
        }

        int CalculateAvailableCapacity(InventoryGroup group, int itemId, int maxStack)
        {
            int capacity = 0;
            for (int i = 0; i < group.slots.Length; i++)
            {
                if (group.slots[i].itemId == itemId)
                {
                    capacity += Mathf.Max(0, maxStack - group.slots[i].count);
                }
                else if (group.slots[i].itemId == -1)
                {
                    capacity += maxStack;
                }
            }

            return capacity;
        }

        int AddToGroupInternal(InventoryGroup group, ItemData item, int count)
        {
            int remaining = count;
            for (int i = 0; i < group.slots.Length && remaining > 0; i++)
            {
                if (group.slots[i].itemId == item.itemId && group.slots[i].count < item.maxStack)
                {
                    int add = Mathf.Min(remaining, item.maxStack - group.slots[i].count);
                    group.slots[i].count += add;
                    remaining -= add;
                }
            }

            for (int i = 0; i < group.slots.Length && remaining > 0; i++)
            {
                if (group.slots[i].itemId == -1)
                {
                    int add = Mathf.Min(remaining, item.maxStack);
                    group.slots[i].itemId = item.itemId;
                    group.slots[i].count = add;
                    remaining -= add;
                }
            }

            int added = count - remaining;
            group.currentWeight += item.weight * added;
            return added;
        }

        public void ForceRecalculateWeight()
        {
            if (groups == null)
            {
                return;
            }

            for (int i = 0; i < groups.Count; i++)
            {
                RecalculateWeight(groups[i]);
            }
        }

        bool PickUpFromSlot(SU_SlotTemplate slotTemplate, int count, bool fromDrag)
        {
            InventoryGroup group = GetGroup(slotTemplate.groupIndex);
            if (group == null || itemDataBase == null)
            {
                return false;
            }

            Slot slot = group.slots[slotTemplate.slotIndex];
            if (slot.itemId == -1 || slot.count <= 0)
            {
                return false;
            }

            ItemData item = itemDataBase.GetItem(slot.itemId);
            if (item == null)
            {
                return false;
            }

            int take = Mathf.Min(count, slot.count);
            if (take <= 0)
            {
                return false;
            }

            heldState = new HeldState
            {
                itemId = slot.itemId,
                count = take,
                sourceGroupIndex = slotTemplate.groupIndex,
                sourceSlotIndex = slotTemplate.slotIndex,
                fromDrag = fromDrag,
                dropHandled = false
            };

            UpdateSelectedItemUI(item);

            slot.count -= take;
            if (slot.count <= 0)
            {
                slot.itemId = -1;
                slot.count = 0;
            }

            group.currentWeight = Mathf.Max(0f, group.currentWeight - item.weight * take);
            UpdateSlotsUI(group);
            UpdateGroupWeightUI(group);
            return true;
        }

        bool TryPlaceHeldOnSlot(SU_SlotTemplate slotTemplate, int requestedCount, bool allowPartial)
        {
            InventoryGroup targetGroup = GetGroup(slotTemplate.groupIndex);
            InventoryGroup sourceGroup = GetGroup(heldState.sourceGroupIndex);
            if (targetGroup == null || itemDataBase == null)
            {
                return false;
            }

            ItemData item = itemDataBase.GetItem(heldState.itemId);
            if (item == null)
            {
                return false;
            }

            Slot targetSlot = targetGroup.slots[slotTemplate.slotIndex];
            int remaining = heldState.count;
            int toMove = Mathf.Min(remaining, requestedCount);
            if (toMove <= 0)
            {
                return false;
            }

            if (targetSlot.itemId != -1 && targetSlot.itemId != heldState.itemId)
            {
                return false;
            }

            int available = targetSlot.itemId == -1 ? item.maxStack : item.maxStack - targetSlot.count;
            int place = Mathf.Min(toMove, available);
            if (place <= 0)
            {
                return false;
            }

            if (!allowPartial && place < toMove)
            {
                return false;
            }

            if (targetGroup != sourceGroup)
            {
                float totalWeight = targetGroup.currentWeight + (item.weight * place);
                if (totalWeight > targetGroup.maxWeight)
                {
                    return false;
                }
            }

            if (targetSlot.itemId == -1)
            {
                targetSlot.itemId = heldState.itemId;
                targetSlot.count = place;
            }
            else
            {
                targetSlot.count += place;
            }

            heldState.count -= place;
            targetGroup.currentWeight += item.weight * place;

            SetSelectedSlot(slotTemplate);
            UpdateSelectedItemUI(item);

            UpdateSlotsUI(targetGroup);
            UpdateGroupWeightUI(targetGroup);

            if (heldState.count <= 0)
            {
                ClearHeld();
            }

            return true;
        }

        void ReturnHeldToSource()
        {
            if (!IsHolding)
            {
                return;
            }

            InventoryGroup sourceGroup = GetGroup(heldState.sourceGroupIndex);
            if (sourceGroup == null || itemDataBase == null)
            {
                ClearHeld();
                return;
            }

            ItemData item = itemDataBase.GetItem(heldState.itemId);
            if (item == null)
            {
                ClearHeld();
                return;
            }

            Slot sourceSlot = sourceGroup.slots[heldState.sourceSlotIndex];
            int remaining = heldState.count;
            int placedInSlot = 0;

            if (sourceSlot.itemId == -1)
            {
                int add = Mathf.Min(remaining, item.maxStack);
                sourceSlot.itemId = heldState.itemId;
                sourceSlot.count = add;
                remaining -= add;
                placedInSlot = add;
            }
            else if (sourceSlot.itemId == heldState.itemId && sourceSlot.count < item.maxStack)
            {
                int add = Mathf.Min(remaining, item.maxStack - sourceSlot.count);
                sourceSlot.count += add;
                remaining -= add;
                placedInSlot = add;
            }

            if (placedInSlot > 0)
            {
                sourceGroup.currentWeight += item.weight * placedInSlot;
            }

            if (remaining > 0)
            {
                int added = AddToGroupInternal(sourceGroup, item, remaining);
                remaining -= added;
            }

            heldState.count = remaining;

            UpdateSlotsUI(sourceGroup);
            UpdateGroupWeightUI(sourceGroup);

            if (heldState.count <= 0)
            {
                ClearHeld();
            }
        }

        void ClearHeld()
        {
            heldState = new HeldState { itemId = -1 };
            UpdateDragIcon();
        }

        void UpdateDragIcon()
        {
            if (dragIcon == null)
            {
                return;
            }

            if (!IsHolding || itemDataBase == null)
            {
                dragIcon.enabled = false;
                return;
            }

            ItemData item = itemDataBase.GetItem(heldState.itemId);
            if (item == null)
            {
                dragIcon.enabled = false;
                return;
            }

            dragIcon.enabled = true;
            dragIcon.sprite = item.icon;
            Color color = dragIcon.color;
            color.a = dragIconAlpha;
            dragIcon.color = color;

            Vector2 mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            Canvas canvas = dragIcon.canvas;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                RectTransform canvasRect = canvas.transform as RectTransform;
                if (canvasRect != null)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePosition, canvas.worldCamera, out var localPoint);
                    dragIcon.rectTransform.position = canvasRect.TransformPoint(localPoint);
                }
                else
                {
                    dragIcon.rectTransform.position = mousePosition;
                }
            }
            else
            {
                dragIcon.rectTransform.position = mousePosition;
            }

            dragIcon.transform.SetAsLastSibling();
        }

        void RecalculateWeight(InventoryGroup group)
        {
            group.currentWeight = 0f;
            if (group.slots == null || itemDataBase == null)
            {
                return;
            }

            for (int i = 0; i < group.slots.Length; i++)
            {
                if (group.slots[i].itemId == -1 || group.slots[i].count == 0)
                {
                    continue;
                }

                ItemData item = itemDataBase.GetItem(group.slots[i].itemId);
                if (item != null)
                {
                    group.currentWeight += item.weight * group.slots[i].count;
                }
            }
        }

        void UpdateSlotsUI(InventoryGroup group)
        {
            if (group == null || group.slots == null || itemDataBase == null)
            {
                return;
            }

            for (int i = 0; i < group.slots.Length; i++)
            {
                if (group.slots[i].slot == null)
                {
                    continue;
                }

                ItemData item = group.slots[i].itemId != -1 ? itemDataBase.GetItem(group.slots[i].itemId) : null;
                if (item != null)
                {
                    group.slots[i].slot.item.enabled = true;
                    group.slots[i].slot.item.sprite = item.icon;

                    if (group.slots[i].count > 1)
                    {
                        group.slots[i].slot.count.enabled = true;
                        group.slots[i].slot.count.text = group.slots[i].count.ToString();
                    }
                    else
                    {
                        group.slots[i].slot.count.enabled = false;
                    }
                }
                else
                {
                    group.slots[i].slot.item.enabled = false;
                    group.slots[i].slot.count.enabled = false;
                }
            }
        }

        void UpdateGroupWeightUI(InventoryGroup group)
        {
            if (group == null)
            {
                return;
            }

            if (group.weightText != null)
            {
                group.weightText.text = $"{group.currentWeight:0.##}/{group.maxWeight:0.##}";
                group.weightText.color = group.currentWeight >= group.maxWeight ? Color.red : group.currentWeightDefaultColor;
            }
        }

        void UpdateSelectedItemUI(ItemData item)
        {
            if (selectedItemNameText != null)
            {
                selectedItemNameText.text = item != null ? $"名前：{item.itemName}" : "名前：";
            }

            if (selectedItemWeightText != null)
            {
                selectedItemWeightText.text = item != null ? $"重量：{item.weight:0.##}" : "重量：";
            }

            if (selectedItemDescriptionText != null)
            {
                selectedItemDescriptionText.text = item != null ? $"説明：{item.description}" : "説明：";
            }
        }
    }
}
