using UnityEngine;

namespace BITROOT.Inventory
{
    public enum ItemCategory
    {
        Weapon,
        Consumable,
        CraftingMaterial,
        QuestItem,
        Misc
    }

    /// <summary>
    /// Base data-only definition for anything that can live in the inventory.
    /// Concrete item types (WeaponData, ConsumableData, MaterialData) extend this.
    /// Kept as ScriptableObjects so designers can create items as assets without touching code.
    /// </summary>
    public abstract class ItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemId;              // unique key, used for save data & crafting lookups
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public ItemCategory category;

        [Header("Stacking")]
        public bool stackable = true;
        public int maxStackSize = 99;

        [Header("Economy (optional)")]
        public int sellValue = 0;
    }
}
