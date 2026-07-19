using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BITROOT.Crafting;

namespace BITROOT.Inventory
{
    [Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int count;

        public InventorySlot(ItemData item, int count)
        {
            this.item = item;
            this.count = count;
        }

        public bool IsEmpty => item == null || count <= 0;
    }
    public class InventorySystem : MonoBehaviour
    {
        [SerializeField] private int slotCapacity = 40;
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

        public IReadOnlyList<InventorySlot> Slots => slots;

        public event Action OnInventoryChanged;
        public event Action<ItemData, int> OnItemAdded;
        public event Action<ItemData, int> OnItemRemoved;
        public event Action OnInventoryFull;
        public int AddItem(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return 0;
            int remaining = amount;

            if (item.stackable)
            {
                foreach (var slot in slots)
                {
                    if (remaining <= 0) break;
                    if (slot.item == item && slot.count < item.maxStackSize)
                    {
                        int space = item.maxStackSize - slot.count;
                        int toAdd = Mathf.Min(space, remaining);
                        slot.count += toAdd;
                        remaining -= toAdd;
                    }
                }
            }

            while (remaining > 0 && slots.Count < slotCapacity)
            {
                int stackAmount = item.stackable ? Mathf.Min(item.maxStackSize, remaining) : 1;
                slots.Add(new InventorySlot(item, stackAmount));
                remaining -= stackAmount;

                if (!item.stackable && remaining > 0 && slots.Count >= slotCapacity)
                    break;
            }

            int added = amount - remaining;
            if (added > 0)
            {
                OnItemAdded?.Invoke(item, added);
                OnInventoryChanged?.Invoke();
            }
            if (remaining > 0)
            {
                OnInventoryFull?.Invoke();
            }
            return added;
        }

        public int RemoveItem(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return 0;
            int remaining = amount;

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (remaining <= 0) break;
                var slot = slots[i];
                if (slot.item != item) continue;

                int toRemove = Mathf.Min(slot.count, remaining);
                slot.count -= toRemove;
                remaining -= toRemove;

                if (slot.count <= 0) slots.RemoveAt(i);
            }

            int removed = amount - remaining;
            if (removed > 0)
            {
                OnItemRemoved?.Invoke(item, removed);
                OnInventoryChanged?.Invoke();
            }
            return removed;
        }

        public int GetItemCount(ItemData item)
        {
            return slots.Where(s => s.item == item).Sum(s => s.count);
        }

        public bool HasItem(ItemData item, int amount = 1)
        {
            return GetItemCount(item) >= amount;
        }

        public bool HasIngredients(IEnumerable<CraftingIngredient> ingredients)
        {
            return ingredients.All(ing => HasItem(ing.item, ing.amount));
        }
    }
}
