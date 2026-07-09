using UnityEngine;
using BITROOT.Inventory;

namespace BITROOT.Combat
{
    /// <summary>
    /// Reads raw input and drives WeaponManager / ConsumableUser. Kept as its own
    /// component (not baked into WeaponManager) so swapping to the new Input System
    /// later only means rewriting this one file.
    ///
    /// Bindings:
    ///   Hold  Left Alt      -> open weapon wheel, scroll or 1/2/3 to highlight, release to equip
    ///   Press X             -> use best available fast-heal consumable
    ///   Press Mouse3 (wheel click) -> quick-throw grenade without swapping equipped weapon
    /// </summary>
    [RequireComponent(typeof(WeaponManager))]
    public class PlayerCombatInput : MonoBehaviour
    {
        [SerializeField] private WeaponManager weaponManager;
        [SerializeField] private ConsumableUser consumableUser;
        [SerializeField] private InventorySystem inventory;
        [SerializeField] private WeaponWheelUI weaponWheelUI; // optional; input still works without it

        [Header("Fast Heal")]
        [Tooltip("Preferred consumable to use with X. If it's out, falls back to the first FastHeal item found in the inventory.")]
        [SerializeField] private ConsumableData preferredHealItem;

        private void Reset()
        {
            weaponManager = GetComponent<WeaponManager>();
            consumableUser = GetComponent<ConsumableUser>();
            inventory = GetComponent<InventorySystem>();
        }

        private void Update()
        {
            HandleWeaponWheel();
            HandleFastHeal();
            HandleQuickGrenade();
            HandleFireInput();
        }

        private void HandleWeaponWheel()
        {
            if (Input.GetKeyDown(KeyCode.LeftAlt))
            {
                weaponWheelUI?.Open();
            }

            if (Input.GetKey(KeyCode.LeftAlt) && weaponWheelUI != null && weaponWheelUI.IsOpen)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll > 0f) weaponWheelUI.HighlightNext();
                else if (scroll < 0f) weaponWheelUI.HighlightPrevious();

                if (Input.GetKeyDown(KeyCode.Alpha1)) weaponWheelUI.HighlightIndex(0);
                if (Input.GetKeyDown(KeyCode.Alpha2)) weaponWheelUI.HighlightIndex(1);
                if (Input.GetKeyDown(KeyCode.Alpha3)) weaponWheelUI.HighlightIndex(2);
            }

            if (Input.GetKeyUp(KeyCode.LeftAlt))
            {
                weaponWheelUI?.ConfirmAndClose();
            }
        }

        private void HandleFastHeal()
        {
            if (!Input.GetKeyDown(KeyCode.X)) return;
            if (consumableUser == null) return;

            if (preferredHealItem != null && inventory != null && inventory.HasItem(preferredHealItem, 1))
            {
                consumableUser.UseConsumable(preferredHealItem);
                return;
            }

            // Fall back to whatever fast-heal item is actually in the bag.
            if (inventory == null) return;
            foreach (var slot in inventory.Slots)
            {
                if (slot.item is ConsumableData consumable && consumable.effect == ConsumableEffect.FastHeal)
                {
                    consumableUser.UseConsumable(consumable);
                    break;
                }
            }
        }

        private void HandleQuickGrenade()
        {
            if (Input.GetMouseButtonDown(2)) // middle mouse / scroll wheel press
            {
                weaponManager?.QuickThrowGrenade();
            }
        }

        private void HandleFireInput()
        {
            // Skip firing input entirely while the weapon wheel is open so a held
            // trigger doesn't fire the old weapon while you're picking a new one.
            if (weaponWheelUI != null && weaponWheelUI.IsOpen) return;

            if (Input.GetMouseButtonDown(0)) weaponManager.FirePrimary();
            if (Input.GetButtonDown("Fire2")) weaponManager.FireSecondary();
            if (Input.GetMouseButton(0)) weaponManager.HoldTrigger();
            if (Input.GetMouseButtonUp(0)) weaponManager.ReleaseTrigger();
            if (Input.GetKeyDown(KeyCode.R)) weaponManager.Reload();
        }
    }
}
