using System.Collections;
using UnityEngine;
using BITROOT.Health;
using BITROOT.Combat;

namespace BITROOT.Inventory
{
    /// <summary>
    /// Put this on the player alongside InventorySystem, HealthSystem, and WeaponManager.
    /// Translates "use this consumable" into the right effect without those systems
    /// needing to know consumables exist at all.
    /// </summary>
    [RequireComponent(typeof(InventorySystem))]
    public class ConsumableUser : MonoBehaviour
    {
        [SerializeField] private HealthSystem health;
        [SerializeField] private WeaponManager weaponManager;
        private InventorySystem inventory;

        private void Awake()
        {
            inventory = GetComponent<InventorySystem>();
            if (health == null) health = GetComponent<HealthSystem>();
            if (weaponManager == null) weaponManager = GetComponent<WeaponManager>();
        }

        /// <summary>
        /// Attempts to use one unit of the given consumable from the inventory.
        /// Returns false if the player doesn't have it or the effect can't apply right now.
        /// </summary>
        public bool UseConsumable(ConsumableData item)
        {
            if (item == null || !inventory.HasItem(item, 1)) return false;

            switch (item.effect)
            {
                case ConsumableEffect.FastHeal:
                    if (health == null || health.IsDead || health.CurrentHealth >= health.MaxHealth) return false;
                    if (health.IsFastHealing) return false;
                    inventory.RemoveItem(item, 1);
                    health.StartFastHeal(item.value);
                    return true;

                case ConsumableEffect.InstantHeal:
                    if (health == null || health.IsDead) return false;
                    inventory.RemoveItem(item, 1);
                    health.Heal(item.value);
                    return true;

                case ConsumableEffect.HealOverTime:
                    if (health == null || health.IsDead) return false;
                    inventory.RemoveItem(item, 1);
                    StartCoroutine(HealOverTimeRoutine(item.value, item.duration));
                    return true;

                case ConsumableEffect.AmmoRefill:
                    if (weaponManager == null || !(weaponManager.CurrentWeapon is Gun gun)) return false;
                    inventory.RemoveItem(item, 1);
                    gun.AddReserveAmmo(Mathf.RoundToInt(item.value));
                    return true;

                case ConsumableEffect.TemporaryBuff:
                    // Hook into a buff/status-effect system here once one exists.
                    inventory.RemoveItem(item, 1);
                    return true;

                default:
                    return false;
            }
        }

        private IEnumerator HealOverTimeRoutine(float totalAmount, float duration)
        {
            if (duration <= 0f)
            {
                health.Heal(totalAmount);
                yield break;
            }

            float elapsed = 0f;
            float healedSoFar = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float targetHealed = totalAmount * (elapsed / duration);
                float tick = targetHealed - healedSoFar;
                if (tick > 0f)
                {
                    health.Heal(tick, silent: true);
                    healedSoFar += tick;
                }
                yield return null;
            }
        }
    }
}
