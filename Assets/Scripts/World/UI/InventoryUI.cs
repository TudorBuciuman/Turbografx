using System.Collections.Generic;
using UnityEngine;
using BITROOT.Inventory;
using BITROOT.Combat;

namespace BITROOT.UI
{
    /// <summary>
    /// The full inventory screen. Listens to InventorySystem's events and rebuilds
    /// the grid - it never polls. Clicking a slot equips weapons or uses consumables;
    /// everything else (materials, quest items) just shows info.
    ///
    /// Hierarchy expected:
    ///   InventoryPanel (this component, starts inactive)
    ///     - Background
    ///     - Header (title text, close button)
    ///     - SlotGrid (GridLayoutGroup, parent for instantiated InventorySlotUI prefabs)
    ///     - Tooltip (ItemTooltipUI)
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform slotGridParent;
        [SerializeField] private InventorySlotUI slotPrefab;
        [SerializeField] private ItemTooltipUI tooltip;

        [SerializeField] private InventorySystem inventory;
        [SerializeField] private WeaponManager weaponManager;
        [SerializeField] private ConsumableUser consumableUser;

        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

        private readonly List<InventorySlotUI> spawnedSlots = new List<InventorySlotUI>();
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (inventory != null) inventory.OnInventoryChanged += Refresh;
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.OnInventoryChanged -= Refresh;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                if (IsOpen) Close(); else Open();
            }
        }

        public void Open()
        {
            IsOpen = true;
            if (panelRoot != null) panelRoot.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Refresh();
        }

        public void Close()
        {
            IsOpen = false;
            if (panelRoot != null) panelRoot.SetActive(false);
            tooltip?.Hide();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>Rebuilds every slot tile from the current inventory contents.</summary>
        private void Refresh()
        {
            if (inventory == null || slotGridParent == null || slotPrefab == null) return;

            var slotsData = inventory.Slots;

            // Grow the pool if the inventory has more entries than we've spawned tiles for.
            while (spawnedSlots.Count < slotsData.Count)
            {
                var newSlot = Instantiate(slotPrefab, slotGridParent);
                WireSlotEvents(newSlot);
                spawnedSlots.Add(newSlot);
            }

            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                if (i < slotsData.Count)
                {
                    spawnedSlots[i].gameObject.SetActive(true);
                    spawnedSlots[i].Bind(slotsData[i].item, slotsData[i].count);
                }
                else
                {
                    spawnedSlots[i].gameObject.SetActive(false);
                    spawnedSlots[i].Bind(null, 0);
                }
            }
        }

        private void WireSlotEvents(InventorySlotUI slot)
        {
            slot.OnClicked += HandleSlotClicked;
            slot.OnHoverEnter += s => tooltip?.Show(s.BoundItem);
            slot.OnHoverExit += _ => tooltip?.Hide();
        }

        private void HandleSlotClicked(InventorySlotUI slot)
        {
            if (slot.BoundItem == null) return;

            switch (slot.BoundItem)
            {
                case WeaponData weapon when weaponManager != null:
                    if (weaponManager.TryGetSlotIndex(weapon.weaponType, out int index))
                        weaponManager.SwitchTo(index);
                    break;

                case ConsumableData consumable when consumableUser != null:
                    consumableUser.UseConsumable(consumable);
                    break;

                default:
                    // Materials / quest items: no click action, tooltip is enough.
                    break;
            }
        }
    }
}
