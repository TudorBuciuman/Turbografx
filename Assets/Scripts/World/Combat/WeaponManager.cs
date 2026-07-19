using System;
using System.Collections.Generic;
using UnityEngine;
using BITROOT.Inventory;

namespace BITROOT.Combat
{
    public class WeaponManager : MonoBehaviour
    {
        [Serializable]
        public class WeaponSlot
        {
            public WeaponData data;
            public WeaponBase instance; 
        }

        [SerializeField] private Transform weaponSocket;
        [SerializeField] public AudioSource audioSource;
        [SerializeField] private List<WeaponSlot> slots = new List<WeaponSlot>();
        [SerializeField] private int startingSlotIndex = 0;

        private int currentIndex = -1;

        public WeaponBase CurrentWeapon
        {
            get
            {
                if (currentIndex >= 0 && currentIndex < slots.Count)
                {
                    if (slots[currentIndex].instance != null)
                    {
                        return slots[currentIndex].instance;
                    }
                }
                return null;
            }
        }

        public event Action<WeaponBase> OnWeaponSwitched;
        public event Action<WeaponData> OnWeaponAcquired;

        private void Start()
        {
            InitializePreConfiguredWeapons();

            if (slots.Count > 0)
            {
                int targetIndex = Mathf.Clamp(startingSlotIndex, 0, slots.Count - 1);

                currentIndex = -1;
                SwitchTo(targetIndex);
            }
        }

        private void InitializePreConfiguredWeapons()
        {
            GameObject ownerObject = this.gameObject;

            foreach (var slot in slots)
            {
                if (slot.data == null) continue;
                if (slot.data.worldPrefab == null)
                {
                    continue;
                }

                GameObject go = Instantiate(slot.data.worldPrefab, weaponSocket);
                Debug.Log(slot.data.displayName);
                if (go.TryGetComponent<WeaponBase>(out var weaponInstance))
                {
                    weaponInstance.Initialize(slot.data, ownerObject);
                    weaponInstance.gameObject.SetActive(false);
                    slot.instance = weaponInstance;
                }
                else
                {
                    Debug.LogError("Prefab for " +slot.data.displayName+" is missing a WeaponBase component");
                    Destroy(go);
                }
            }
        }
        public void AcquireWeapon(WeaponData weaponData, GameObject owner)
        {
            foreach (var slot in slots)
            {
                if (slot.data == weaponData) return; 
            }

            GameObject go = Instantiate(weaponData.worldPrefab, weaponSocket);
            if (!go.TryGetComponent<WeaponBase>(out var weaponInstance))
            {
                Debug.LogError("Weapon prefab for "+weaponData.displayName+" has no WeaponBase component.");
                Destroy(go);
                return;
            }

            weaponInstance.Initialize(weaponData, owner);
            weaponInstance.gameObject.SetActive(false);

            slots.Add(new WeaponSlot { data = weaponData, instance = weaponInstance });
            OnWeaponAcquired?.Invoke(weaponData);

            if (currentIndex < 0)
                SwitchTo(slots.Count - 1);
        }

        public void SwitchTo(int index)
        {
            if (index < 0 || index >= slots.Count || index == currentIndex) return;

            Debug.Log($"Switching to weapon slot {index} ({slots[index].data.displayName})");
            CurrentWeapon?.Unequip();
            currentIndex = index;
            CurrentWeapon?.Equip();
            OnWeaponSwitched?.Invoke(CurrentWeapon);
        }

        public void SwitchToType(WeaponType type)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].data.weaponType == type)
                {
                    SwitchTo(i);
                    return;
                }
            }
        }

        public void CycleNext()
        {
            if (slots.Count == 0) return;
            SwitchTo((currentIndex + 1) % slots.Count);
        }

        public void CyclePrevious()
        {
            if (slots.Count == 0) return;
            SwitchTo((currentIndex - 1 + slots.Count) % slots.Count);
        }
        public void FirePrimary() => CurrentWeapon?.PrimaryAction();
        public void FireSecondary() => CurrentWeapon?.SecondaryAction();

        public void HoldTrigger()
        {
            if (CurrentWeapon is Gun gun) gun.TriggerDown();
        }

        public void ReleaseTrigger()
        {
            if (CurrentWeapon is Gun gun) gun.TriggerUp();
        }

        public void Reload()
        {
            if (CurrentWeapon is Gun gun) gun.Reload();
        }

        public void QuickThrowGrenade()
        {
            int grenadeIndex = -1;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].data.weaponType == WeaponType.Grenade)
                {
                    grenadeIndex = i;
                    break;
                }
            }
            if (grenadeIndex < 0) return; 

            if (slots[grenadeIndex].instance is Grenade grenade && grenade.CarriedCount <= 0)
                return; 

            int previousIndex = currentIndex;
            bool switchedAway = previousIndex != grenadeIndex;

            if (switchedAway)
            {
                CurrentWeapon?.Unequip();
                currentIndex = grenadeIndex;
                CurrentWeapon?.Equip();
            }

            CurrentWeapon?.PrimaryAction();

            if (switchedAway)
            {
                CurrentWeapon?.Unequip();
                currentIndex = previousIndex;
                CurrentWeapon?.Equip();
                OnWeaponSwitched?.Invoke(CurrentWeapon);
            }
        }

        public List<WeaponData> GetOwnedWeapons()
        {
            var result = new List<WeaponData>(slots.Count);
            foreach (var slot in slots) result.Add(slot.data);
            return result;
        }
        public bool TryGetSlotIndex(WeaponType type, out int index)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].data.weaponType == type)
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }
    }
}
