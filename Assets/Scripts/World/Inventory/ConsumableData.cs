using UnityEngine;

namespace BITROOT.Inventory
{
    public enum ConsumableEffect
    {
        FastHeal,
        InstantHeal,
        HealOverTime,
        AmmoRefill,
        TemporaryBuff
    }

    [CreateAssetMenu(menuName = "Turbografx/Items/Consumable", fileName = "New Consumable")]
    public class ConsumableData : ItemData
    {
        [Header("Consumable")]
        public ConsumableEffect effect = ConsumableEffect.FastHeal;
        public float value = 30f;          // heal amount / ammo amount / buff magnitude
        public float duration = 0f;        // for HoT or buffs, 0 = instant
        public float useTime = 1.2f;       // animation / channel time before effect applies
        public AudioClip useSound;
    }

    [CreateAssetMenu(menuName = "Turbografx/Items/Crafting Material", fileName = "New Material")]
    public class MaterialData : ItemData
    {
        [Header("Crafting Material")]
        [Tooltip("Purely descriptive - e.g. 'Scrap Metal', 'Circuit Board', 'Neon Coolant'.")]
        public string materialTier = "Common";
    }
}
