using System;
using System.Collections;
using UnityEngine;

namespace BITROOT.Health
{
    /// <summary>
    /// Generic damageable interface so weapons, hazards, and explosions
    /// don't need to know whether they're hitting the player or an enemy.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float amount, GameObject source, DamageType type = DamageType.Generic);
        bool IsDead { get; }
    }

    public enum DamageType
    {
        Generic,
        Bullet,
        Melee,
        Explosion,
        Environmental
    }

    /// <summary>
    /// Drop this on the player or any enemy. Decoupled via System.Action events —
    /// UI, cutscene director, and AI all subscribe instead of being referenced directly.
    /// </summary>
    public class HealthSystem : MonoBehaviour, IDamageable
    {
        [Header("Base Stats")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private bool invulnerable = false;

        [Header("Regen (passive, optional)")]
        [SerializeField] private bool enablePassiveRegen = false;
        [SerializeField] private float regenPerSecond = 2f;
        [SerializeField] private float regenDelayAfterDamage = 4f;

        [Header("Fast Heal (item-triggered)")]
        [Tooltip("How long a 'fast heal' item takes to apply, e.g. a med-kit animation window.")]
        [SerializeField] private float fastHealDuration = 1.2f;
        [SerializeField] private bool canFastHealWhileMoving = true;

        private float lastDamageTime;
        private Coroutine regenRoutine;
        private Coroutine fastHealRoutine;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercent => maxHealth <= 0f ? 0f : currentHealth / maxHealth;
        public bool IsDead { get; private set; }
        public bool IsFastHealing { get; private set; }

        // Decoupled event hooks
        public event Action<float, float> OnHealthChanged;   // (current, max)
        public event Action<float, GameObject, DamageType> OnDamaged; // (amount, source, type)
        public event Action OnDeath;
        public event Action OnRevive;
        public event Action<float> OnFastHealStarted;   // duration
        public event Action<float> OnFastHealCompleted; // amount healed

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        private void Update()
        {
            if (enablePassiveRegen && !IsDead && currentHealth < maxHealth)
            {
                if (Time.time - lastDamageTime >= regenDelayAfterDamage)
                {
                    Heal(regenPerSecond * Time.deltaTime, silent: true);
                }
            }
        }

        public void TakeDamage(float amount, GameObject source, DamageType type = DamageType.Generic)
        {
            if (IsDead || invulnerable || amount <= 0f) return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            lastDamageTime = Time.time;

            // Getting hit interrupts a fast heal in progress.
            if (fastHealRoutine != null)
            {
                StopCoroutine(fastHealRoutine);
                fastHealRoutine = null;
                IsFastHealing = false;
            }

            OnDamaged?.Invoke(amount, source, type);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount, bool silent = false)
        {
            if (IsDead || amount <= 0f) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            if (!silent) OnHealthChanged?.Invoke(currentHealth, maxHealth);
            else OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Call this from a consumable item (med-kit, nano-stim, etc).
        /// Applies a short delay then heals a flat amount, matching an animation/use-time.
        /// </summary>
        public void StartFastHeal(float healAmount)
        {
            if (IsDead || IsFastHealing) return;
            if (currentHealth >= maxHealth) return;

            fastHealRoutine = StartCoroutine(FastHealRoutine(healAmount));
        }

        private IEnumerator FastHealRoutine(float healAmount)
        {
            IsFastHealing = true;
            OnFastHealStarted?.Invoke(fastHealDuration);

            float t = 0f;
            while (t < fastHealDuration)
            {
                t += Time.deltaTime;
                // If damage interrupts it, TakeDamage() will null the routine and stop this loop next Update.
                yield return null;
            }

            Heal(healAmount);
            IsFastHealing = false;
            fastHealRoutine = null;
            OnFastHealCompleted?.Invoke(healAmount);
        }

        public void Revive(float reviveHealthPercent = 0.5f)
        {
            if (!IsDead) return;
            IsDead = false;
            currentHealth = maxHealth * Mathf.Clamp01(reviveHealthPercent);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnRevive?.Invoke();
        }

        public void SetInvulnerable(bool value) => invulnerable = value;

        private void Die()
        {
            IsDead = true;
            OnDeath?.Invoke();
        }
    }
}
