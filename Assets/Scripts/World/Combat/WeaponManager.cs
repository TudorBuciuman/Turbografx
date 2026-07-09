using System;
using System.Collections.Generic;
using UnityEngine;
using BITROOT.Inventory;

namespace BITROOT.Combat
{
    /// <summary>
    /// Central point for equipping/switching weapons. Player input and UI
    /// only ever talk to this class - never to Gun/Katana/Grenade directly -
    /// so adding a new input scheme or weapon wheel UI doesn't ripple outward.
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        [Serializable]
        public class WeaponSlot
        {
            public WeaponData data;
            public WeaponBase instance; // spawned/enabled prefab living under weaponSocket
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
                    // Explicitly using != null forces Unity's custom lifetime check
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
            // 1. Initialize any weapons that were pre-configured in the Unity Inspector
            InitializePreConfiguredWeapons();

            // 2. Safely switch to your starting weapon slot
            if (slots.Count > 0)
            {
                int targetIndex = Mathf.Clamp(startingSlotIndex, 0, slots.Count - 1);

                // Force the current index to something else so SwitchTo doesn't early-return
                currentIndex = -1;
                SwitchTo(targetIndex);
            }
        }

        private void InitializePreConfiguredWeapons()
        {
            // The owner of the weapons is usually the GameObject this manager is attached to
            GameObject ownerObject = this.gameObject;

            foreach (var slot in slots)
            {
                // Skip if there's no data, or if an instance already somehow exists
                if (slot.data == null) continue;
                if (slot.data.worldPrefab == null)
                {
                    continue;
                }

                // Spawn the weapon prefab directly under the socket
                GameObject go = Instantiate(slot.data.worldPrefab, weaponSocket);
                Debug.Log(slot.data.displayName);
                if (go.TryGetComponent<WeaponBase>(out var weaponInstance))
                {
                    // Set up the weapon (this is where Guns can set up their starting ammo)
                    weaponInstance.Initialize(slot.data, ownerObject);

                    // Keep it hidden until explicitly equipped via SwitchTo()
                    weaponInstance.gameObject.SetActive(false);

                    // Assign the freshly spawned instance back to the slot
                    slot.instance = weaponInstance;
                }
                else
                {
                    Debug.LogError($"Prefab for {slot.data.displayName} is missing a WeaponBase component!");
                    Destroy(go);
                }
            }
        }
        /// <summary>
        /// Adds a new weapon type to the loadout (e.g. picked up from the world or crafted).
        /// If a prefab isn't already instantiated for it, spawns one under the socket.
        /// </summary>
        public void AcquireWeapon(WeaponData weaponData, GameObject owner)
        {
            foreach (var slot in slots)
            {
                if (slot.data == weaponData) return; // already owned
            }

            GameObject go = Instantiate(weaponData.worldPrefab, weaponSocket);
            if (!go.TryGetComponent<WeaponBase>(out var weaponInstance))
            {
                Debug.LogError($"Weapon prefab for {weaponData.displayName} has no WeaponBase component.");
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

        // --- Input-facing pass-throughs -------------------------------------

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

        /// <summary>
        /// Throws a grenade from whichever slot holds one without permanently switching
        /// the equipped weapon - Cyberpunk-style "quick grenade" bound to its own key,
        /// independent of whatever gun/blade is currently in hand.
        /// </summary>
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
            if (grenadeIndex < 0) return; // no grenades in the loadout

            if (slots[grenadeIndex].instance is Grenade grenade && grenade.CarriedCount <= 0)
                return; // out of grenades entirely

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

        /// <summary>Returns the WeaponData for every slot currently owned, in slot order - used by the weapon wheel UI.</summary>
        public List<WeaponData> GetOwnedWeapons()
        {
            var result = new List<WeaponData>(slots.Count);
            foreach (var slot in slots) result.Add(slot.data);
            return result;
        }

        /// <summary>Returns true and outputs the slot index for the first weapon of the given type.</summary>
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
