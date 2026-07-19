using System;
using System.Collections.Generic;
using UnityEngine;
using BITROOT.Health;

namespace BITROOT.Combat
{
    public class Katana : WeaponBase
    {
        [SerializeField] private Transform attackOrigin;

        public int ComboStep { get; private set; } = 0;

        public event Action<int> OnComboAdvanced; 
        public event Action OnComboReset;
        public event Action<int> OnSwing;         

        private float comboExpireTime;
        private bool attackInProgress;

        public override void PrimaryAction()
        {
            if (attackInProgress) return; 
            Attack();
        }

        private void Attack()
        {
            if (Time.time > comboExpireTime)
                ComboStep = 0;

            ComboStep = (ComboStep + 1) % Mathf.Max(1, data.comboLength);
            comboExpireTime = Time.time + data.comboWindow;

            bool isFinisher = ComboStep == 0; 
            float damage = data.damage * (isFinisher ? data.finisherMultiplier : 1f);

            int hits = SwingHit(damage);

            OnComboAdvanced?.Invoke(ComboStep);
            OnSwing?.Invoke(hits);
            RaisePrimaryUsed();
        }

        private int SwingHit(float damage)
        {
            Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
            Vector3 forward = attackOrigin != null ? attackOrigin.forward : transform.forward;

            Collider[] hitsInRange = Physics.OverlapSphere(
                origin + forward * (data.attackRange * 0.5f),
                data.attackRadius);

            int hitCount = 0;
            foreach (var col in hitsInRange)
            {
                if (col.gameObject == owner) continue;
                if (col.TryGetComponent<IDamageable>(out var damageable) && !damageable.IsDead)
                {
                    damageable.TakeDamage(damage, owner, DamageType.Melee);
                    hitCount++;
                }
            }
            return hitCount;
        }

        public void ResetCombo()
        {
            ComboStep = 0;
            OnComboReset?.Invoke();
        }

        private void Update()
        {
            if (ComboStep != 0 && Time.time > comboExpireTime)
            {
                ResetCombo();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (data == null) return;
            Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
            Vector3 forward = attackOrigin != null ? attackOrigin.forward : transform.forward;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin + forward * (data.attackRange * 0.5f), data.attackRadius);
        }
    }
}
