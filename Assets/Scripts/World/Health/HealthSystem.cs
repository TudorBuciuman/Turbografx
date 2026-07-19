using System;
using System.Collections;
using UnityEngine;

namespace BITROOT.Health
{
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
    public class HealthSystem : MonoBehaviour, IDamageable
    {
        [Header("Base Stats")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private bool invulnerable = false;

        [SerializeField] private bool enablePassiveRegen = false;
        [SerializeField] private float regenPerSecond = 2f;
        [SerializeField] private float regenDelayAfterDamage = 4f;

        [SerializeField] private float fastHealDuration = 1.2f;

        private float lastDamageTime;
        private Coroutine regenRoutine;
        private Coroutine fastHealRoutine;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercent => maxHealth <= 0f ? 0f : currentHealth / maxHealth;
        public bool IsDead { get; private set; }
        public bool IsFastHealing { get; private set; }

        public event Action<float, float> OnHealthChanged; 
        public event Action<float, GameObject, DamageType> OnDamaged; 
        public event Action OnDeath;
        public event Action OnRevive;
        public event Action<float> OnFastHealStarted;  
        public event Action<float> OnFastHealCompleted; 

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
