using System;
using UnityEngine;
using BITROOT.Health;
using BITROOT.Inventory;

namespace BITROOT.Combat
{
    /// <summary>
    /// Throwable weapon slot. Spawns a GrenadeProjectile that carries its own
    /// fuse + explosion logic, so the thrown object works independently of
    /// whoever threw it (important once it's flying through the world).
    /// </summary>
    public class Grenade : WeaponBase
    {
        [SerializeField] private Transform throwOrigin;
        [SerializeField] private int carriedCount = 1;

        public int CarriedCount => carriedCount;
        public event Action<int> OnCountChanged;
        public event Action OnThrown;

        public override void PrimaryAction()
        {
            Throw();
        }

        public void SetCarriedCount(int count)
        {
            carriedCount = count;
            OnCountChanged?.Invoke(carriedCount);
        }

        private void Throw()
        {
            if (carriedCount <= 0 || data.grenadeProjectilePrefab == null) return;

            Vector3 origin = throwOrigin != null ? throwOrigin.position : transform.position;
            Vector3 direction = throwOrigin != null ? throwOrigin.forward : transform.forward;

            GameObject go = Instantiate(data.grenadeProjectilePrefab, origin, Quaternion.LookRotation(direction));
            if (go.TryGetComponent<GrenadeProjectile>(out var projectile))
            {
                projectile.Launch(owner, data, direction * data.throwForce);
            }
            else if (go.TryGetComponent<Rigidbody>(out var rb))
            {
                // Fallback if the prefab doesn't have GrenadeProjectile attached.
                rb.AddForce(direction * data.throwForce, ForceMode.VelocityChange);
            }

            carriedCount--;
            OnCountChanged?.Invoke(carriedCount);
            RaisePrimaryUsed();
            OnThrown?.Invoke();
        }
    }

    /// <summary>
    /// Lives on the thrown grenade prefab itself. Handles fuse timing,
    /// explosion radius damage with falloff, and cleanup.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GrenadeProjectile : MonoBehaviour
    {
        private GameObject thrower;
        private WeaponData data;
        private float fuseTimer;
        private bool exploded;

        public event Action<Vector3, float> OnExploded; // position, radius - for VFX/camera shake hookup

        public void Launch(GameObject throwingActor, WeaponData weaponData, Vector3 initialVelocity)
        {
            thrower = throwingActor;
            data = weaponData;
            fuseTimer = data.fuseTime;

            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.velocity = initialVelocity;
            }
        }

        private void Update()
        {
            if (exploded) return;
            fuseTimer -= Time.deltaTime;
            if (fuseTimer <= 0f)
            {
                Explode();
            }
        }

        private void Explode()
        {
            exploded = true;

            Collider[] hits = Physics.OverlapSphere(transform.position, data.explosionRadius);
            foreach (var col in hits)
            {
                if (col.gameObject == thrower) continue; // no self-damage; drop this line for FF-on games
                if (!col.TryGetComponent<IDamageable>(out var damageable) || damageable.IsDead) continue;

                float distance = Vector3.Distance(transform.position, col.transform.position);
                float normalizedDist = Mathf.Clamp01(distance / data.explosionRadius);
                float falloff = data.damageFalloff.Evaluate(normalizedDist);
                float finalDamage = data.damage * falloff;

                if (finalDamage > 0f)
                    damageable.TakeDamage(finalDamage, thrower, DamageType.Explosion);
            }

            OnExploded?.Invoke(transform.position, data.explosionRadius);
            // Hook explosion VFX/SFX/camera shake here before destroying.
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            if (data == null) return;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, data.explosionRadius);
        }
    }
}
