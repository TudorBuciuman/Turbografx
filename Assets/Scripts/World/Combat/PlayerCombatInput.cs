using UnityEngine;
using BITROOT.Inventory;

namespace BITROOT.Combat
{
    [RequireComponent(typeof(WeaponManager))]
    public class PlayerCombatInput : MonoBehaviour
    {
        [SerializeField] private WeaponManager weaponManager;
        [SerializeField] private ConsumableUser consumableUser;
        [SerializeField] private InventorySystem inventory;
        [SerializeField] private WeaponWheelUI weaponWheelUI; 
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
            if (Input.GetMouseButtonDown(2)) 
            {
                weaponManager?.QuickThrowGrenade();
            }
        }

        private void HandleFireInput()
        {
            if (weaponWheelUI != null && weaponWheelUI.IsOpen) return;

            if (Input.GetMouseButtonDown(0)) weaponManager.FirePrimary();
            if (Input.GetButtonDown("Fire2")) weaponManager.FireSecondary();
            if (Input.GetMouseButton(0)) weaponManager.HoldTrigger();
            if (Input.GetMouseButtonUp(0)) weaponManager.ReleaseTrigger();
            if (Input.GetKeyDown(KeyCode.R)) weaponManager.Reload();
        }
    }
}
