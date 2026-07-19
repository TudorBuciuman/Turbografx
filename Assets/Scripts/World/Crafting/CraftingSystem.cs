using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BITROOT.Inventory;

namespace BITROOT.Crafting
{
    public class CraftingSystem : MonoBehaviour
    {
        [SerializeField] private InventorySystem inventory;
        [SerializeField] private List<string> unlockedRecipeIds = new List<string>();

        public event Action<CraftingRecipe> OnCraftStarted;
        public event Action<CraftingRecipe> OnCraftCompleted;
        public event Action<CraftingRecipe, string> OnCraftFailed; 

        private void Reset()
        {
            inventory = GetComponent<InventorySystem>();
        }

        public bool IsUnlocked(CraftingRecipe recipe)
        {
            return recipe.isUnlockedByDefault || unlockedRecipeIds.Contains(recipe.recipeId);
        }

        public void UnlockRecipe(string recipeId)
        {
            if (!unlockedRecipeIds.Contains(recipeId))
                unlockedRecipeIds.Add(recipeId);
        }

        public bool CanCraft(CraftingRecipe recipe)
        {
            if (recipe == null || inventory == null) return false;
            if (!IsUnlocked(recipe)) return false;
            return inventory.HasIngredients(recipe.ingredients);
        }
        public bool TryCraft(CraftingRecipe recipe)
        {
            if (!ValidateCraft(recipe, out string reason))
            {
                OnCraftFailed?.Invoke(recipe, reason);
                return false;
            }

            ConsumeIngredients(recipe);
            inventory.AddItem(recipe.resultItem, recipe.resultAmount);
            OnCraftCompleted?.Invoke(recipe);
            return true;
        }

        public void TryCraftTimed(CraftingRecipe recipe)
        {
            if (!ValidateCraft(recipe, out string reason))
            {
                OnCraftFailed?.Invoke(recipe, reason);
                return;
            }

            ConsumeIngredients(recipe);
            OnCraftStarted?.Invoke(recipe);
            StartCoroutine(CraftRoutine(recipe));
        }

        private IEnumerator CraftRoutine(CraftingRecipe recipe)
        {
            if (recipe.craftTime > 0f)
                yield return new WaitForSeconds(recipe.craftTime);

            inventory.AddItem(recipe.resultItem, recipe.resultAmount);
            OnCraftCompleted?.Invoke(recipe);
        }

        private bool ValidateCraft(CraftingRecipe recipe, out string reason)
        {
            if (recipe == null) { reason = "No recipe."; return false; }
            if (inventory == null) { reason = "No inventory linked."; return false; }
            if (!IsUnlocked(recipe)) { reason = "Recipe locked."; return false; }
            if (!inventory.HasIngredients(recipe.ingredients)) { reason = "Missing ingredients."; return false; }
            reason = null;
            return true;
        }

        private void ConsumeIngredients(CraftingRecipe recipe)
        {
            foreach (var ing in recipe.ingredients)
            {
                inventory.RemoveItem(ing.item, ing.amount);
            }
        }
    }
}
