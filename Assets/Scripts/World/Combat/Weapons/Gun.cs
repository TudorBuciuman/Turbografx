using System;
using System.Collections;
using UnityEngine;
using BITROOT.Health;

namespace BITROOT.Combat
{
    public class Gun : WeaponBase
    {
        [SerializeField] private Transform muzzle;

        public int CurrentAmmo { get; private set; }
        public int ReserveAmmo { get; private set; }
        public bool IsReloading { get; private set; }

        public event Action<int, int> OnAmmoChanged; // <current, reserve>
        public event Action OnFired;
        public event Action OnReloadStarted;
        public event Action OnReloadCompleted;
        public event Action OnDryFire;

        private float nextFireTime;
        private bool triggerHeld;
        private Coroutine reloadRoutine;
        private Coroutine autoFireRoutine;

        public override void Initialize(Inventory.WeaponData weaponData, GameObject weaponOwner)
        {
            base.Initialize(weaponData, weaponOwner);
            CurrentAmmo = weaponData.magazineSize;
            Debug.Log(data.displayName + " initialized with " + CurrentAmmo + " ammo and " + weaponData.startingReserveAmmo + " reserve ammo.");
            ReserveAmmo = weaponData.startingReserveAmmo;
        }

        public override void PrimaryAction()
        {
            Debug.Log($"PrimaryAction called for {data.displayName} at {Time.time:F2}, next allowed at {nextFireTime:F2}");
            if (data.fireMode == Inventory.FireMode.Automatic)
            {
                TriggerDown();
                return;
            }

            TryFireOnce();
        }

        public void TriggerDown()
        {
            triggerHeld = true;
            if (data.fireMode == Inventory.FireMode.Automatic && autoFireRoutine == null)
                autoFireRoutine = StartCoroutine(AutoFireRoutine());
            else if (data.fireMode != Inventory.FireMode.Automatic)
                TryFireOnce();
        }

        public void TriggerUp()
        {
            triggerHeld = false;
        }

        private IEnumerator AutoFireRoutine()
        {
            while (triggerHeld)
            {
                TryFireOnce();
                yield return null;
            }
            autoFireRoutine = null;
        }

        private void TryFireOnce()
        {
            Debug.Log(CurrentAmmo +" "+IsReloading+" "+nextFireTime);
            if (IsReloading) return;
            if (Time.time < nextFireTime) return;

            if (CurrentAmmo <= 0)
            {
                OnDryFire?.Invoke();
                return;
            }
            Debug.Log($"Firing {data.displayName} at {Time.time:F2}, next allowed at {nextFireTime:F2}");
            nextFireTime = Time.time + (1f / Mathf.Max(0.01f, data.fireRate));
            CurrentAmmo--;
            OnAmmoChanged?.Invoke(CurrentAmmo, ReserveAmmo);

            FireShot();
            RaisePrimaryUsed();
            OnFired?.Invoke();
        }

        private void FireShot()
        {
            Vector3 origin = muzzle != null ? muzzle.position : transform.position;
            Vector3 direction = ApplySpread(muzzle != null ? muzzle.forward : transform.forward);
            FindFirstObjectByType<WeaponManager>().audioSource.clip = Resources.Load<AudioClip>("Sound_effects/Turbografx/pistol");
            FindFirstObjectByType<WeaponManager>().audioSource.Play();
            if (Physics.Raycast(origin, direction, out RaycastHit hit, data.range, data.hitMask))
            {
                if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(data.damage, owner, DamageType.Bullet);
                }                
            }
        }

        private Vector3 ApplySpread(Vector3 forward)
        {
            if (data.bulletSpread <= 0f) return forward;
            float half = data.bulletSpread * 0.5f;
            Quaternion spreadRot = Quaternion.Euler(
                UnityEngine.Random.Range(-half, half),
                UnityEngine.Random.Range(-half, half),
                0f);
            return spreadRot * forward;
        }

        public void Reload()
        {
            if (IsReloading || CurrentAmmo >= data.magazineSize || ReserveAmmo <= 0) return;
            reloadRoutine = StartCoroutine(ReloadRoutine());
        }

        private IEnumerator ReloadRoutine()
        {
            IsReloading = true;
            OnReloadStarted?.Invoke();

            yield return new WaitForSeconds(data.reloadTime);

            int needed = data.magazineSize - CurrentAmmo;
            int toLoad = Mathf.Min(needed, ReserveAmmo);
            CurrentAmmo += toLoad;
            ReserveAmmo -= toLoad;

            IsReloading = false;
            reloadRoutine = null;
            OnAmmoChanged?.Invoke(CurrentAmmo, ReserveAmmo);
            OnReloadCompleted?.Invoke();
        }

        public void AddReserveAmmo(int amount)
        {
            ReserveAmmo += amount;
            OnAmmoChanged?.Invoke(CurrentAmmo, ReserveAmmo);
        }

        public override void Unequip()
        {
            triggerHeld = false;
            if (reloadRoutine != null) StopCoroutine(reloadRoutine);
            if (autoFireRoutine != null) StopCoroutine(autoFireRoutine);
            IsReloading = false;
            base.Unequip();
        }
    }
}
