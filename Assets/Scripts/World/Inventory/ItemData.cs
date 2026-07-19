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

    public abstract class ItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemId;//needs to be uniquwe
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
