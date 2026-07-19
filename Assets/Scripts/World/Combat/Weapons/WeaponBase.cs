using System;
using UnityEngine;
using BITROOT.Inventory;
namespace BITROOT.Combat
{
    public abstract class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected WeaponData data;
        protected GameObject owner;

        public WeaponData Data => data;
        public bool IsEquipped { get; private set; }

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
        public abstract void PrimaryAction();

        public virtual void SecondaryAction() { }

        protected void RaisePrimaryUsed() => OnPrimaryUsed?.Invoke();
    }
}
