using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BITROOT.Inventory;

namespace BITROOT.UI
{
    [RequireComponent(typeof(Image))]
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private Text countText;
        [SerializeField] private Image selectionFrame;

        public ItemData BoundItem { get; private set; }
        public int BoundCount { get; private set; }

        public event System.Action<InventorySlotUI> OnClicked;
        public event System.Action<InventorySlotUI> OnHoverEnter;
        public event System.Action<InventorySlotUI> OnHoverExit;

        private void Reset()
        {
            background = GetComponent<Image>();
        }

        public void Bind(ItemData item, int count)
        {
            BoundItem = item;
            BoundCount = count;

            bool hasItem = item != null && count > 0;

            if (icon != null)
            {
                icon.enabled = hasItem;
                icon.sprite = hasItem ? item.icon : null;
            }

            if (countText != null)
            {
                countText.gameObject.SetActive(hasItem && item.stackable && count > 1);
                countText.text = count.ToString();
            }

            if (background != null)
            {
                background.color = hasItem ? CyberpunkTheme.SlotFilled : CyberpunkTheme.SlotEmpty;
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectionFrame != null)
            {
                selectionFrame.enabled = selected;
                selectionFrame.color = CyberpunkTheme.AccentPrimary;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (BoundItem == null) return;
            OnClicked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (background != null && BoundItem != null)
                background.color = CyberpunkTheme.SlotHover;
            OnHoverEnter?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (background != null)
                background.color = BoundItem != null ? CyberpunkTheme.SlotFilled : CyberpunkTheme.SlotEmpty;
            OnHoverExit?.Invoke(this);
        }
    }
}
