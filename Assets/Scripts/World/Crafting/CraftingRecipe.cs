using System;
using System.Collections.Generic;
using UnityEngine;
using BITROOT.Inventory;

namespace BITROOT.Crafting
{
    [Serializable]
    public struct CraftingIngredient
    {
        public ItemData item;
        public int amount;
    }

    [CreateAssetMenu(menuName = "Turbografx/Crafting/Recipe", fileName = "New Recipe")]
    public class CraftingRecipe : ScriptableObject
    {
        public string recipeId;
        public string displayName;
        public ItemData resultItem;
        public int resultAmount = 1;
        public List<CraftingIngredient> ingredients = new List<CraftingIngredient>();

        [Tooltip("Seconds the crafting bench/UI takes to complete this craft, 0 = instant.")]
        public float craftTime = 0f;

        [Tooltip("Optional: gate this recipe behind a story flag or player level.")]
        public bool isUnlockedByDefault = true;
    }
}
