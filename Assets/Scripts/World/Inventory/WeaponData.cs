using UnityEngine;

namespace BITROOT.Inventory
{
    public enum WeaponType
    {
        Gun,
        Katana,
        Grenade
    }

    public enum FireMode
    {
        Single,
        Burst,
        Automatic
    }

    [CreateAssetMenu(menuName = "Turbografx/Items/Weapon", fileName = "New Weapon")]
    public class WeaponData : ItemData
    {
        [Header("Weapon")]
        public WeaponType weaponType;
        public GameObject worldPrefab;     // dropped-in-world / equipped visual prefab
        public GameObject viewModelPrefab; // optional first/third person arms model

        [Header("Damage")]
        public float damage = 10f;
        public float critMultiplier = 2f;

        [Header("Gun-specific")]
        public FireMode fireMode = FireMode.Single;
        public float fireRate = 8f;          // rounds per second
        public float range = 50f;
        public int magazineSize = 12;
        public int startingReserveAmmo = 60;
        public float reloadTime = 1.6f;
        public float bulletSpread = 1.5f;    // degrees
        public LayerMask hitMask = ~0;

        [Header("Katana-specific")]
        public int comboLength = 3;
        public float comboWindow = 0.8f;     // seconds to chain the next hit
        public float attackRange = 2f;
        public float attackRadius = 0.6f;
        [Tooltip("Damage multiplier applied to the final hit in a combo.")]
        public float finisherMultiplier = 1.75f;

        [Header("Grenade-specific")]
        public GameObject grenadeProjectilePrefab;
        public float fuseTime = 2.5f;
        public float explosionRadius = 5f;
        public float throwForce = 12f;
        public AnimationCurve damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    }
}
