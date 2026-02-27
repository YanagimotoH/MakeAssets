using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SurvivalInventory
{
    public class SU_SlotTemplate : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public Image container;
        public Image item;
        public Text count;
        public Image selectionFrame;

        [HideInInspector]
        public bool hasClicked;
        [HideInInspector]
        public SU_InventoryManager inventoryManager;
        [HideInInspector]
        public int groupIndex;
        [HideInInspector]
        public int slotIndex;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            hasClicked = true;
            if (inventoryManager != null)
            {
                inventoryManager.HandleSlotClick(this, eventData.button);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            if (inventoryManager != null)
            {
                inventoryManager.HandleSlotClick(this, eventData.button);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (inventoryManager != null)
            {
                inventoryManager.BeginDrag(this);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (inventoryManager != null)
            {
                inventoryManager.EndDrag();
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (inventoryManager != null)
            {
                inventoryManager.HandleDrop(this);
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectionFrame != null)
            {
                selectionFrame.enabled = selected;
            }
        }
    }
}
