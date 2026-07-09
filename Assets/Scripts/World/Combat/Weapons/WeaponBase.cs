using System;
using UnityEngine;
using BITROOT.Inventory;
namespace BITROOT.Combat
{
    /// <summary>
    /// Shared contract for anything equippable in a weapon slot.
    /// WeaponManager talks only to this interface, so adding a new weapon type
    /// later (e.g. a bow, a hacking tool) never requires touching the manager.
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected WeaponData data;
        protected GameObject owner;

        public WeaponData Data => data;
        public bool IsEquipped { get; private set; }

        // Decoupled hooks for UI / animation / audio systems.
        public event Action OnEquipped;
        public event Action OnUnequipped;
        public event Action OnPrimaryUsed;

        public virtual void Initialize(WeaponData weaponData, GameObject weaponOwner)
        {
            data = weaponData;
            owner = weaponOwner;
        }

        public virtual void Equip()
        {
            IsEquipped = true;
            gameObject.SetActive(true);
            OnEquipped?.Invoke();
        }

        public virtual void Unequip()
        {
            IsEquipped = false;
            gameObject.SetActive(false);
            OnUnequipped?.Invoke();
        }

        /// <summary>
        /// Primary action: fire the gun, swing the katana, throw the grenade.
        /// </summary>
        public abstract void PrimaryAction();

        /// <summary>
        /// Optional secondary action (ADS, heavy attack, cook-and-throw). Default no-op.
        /// </summary>
        public virtual void SecondaryAction() { }

        protected void RaisePrimaryUsed() => OnPrimaryUsed?.Invoke();
    }
}
