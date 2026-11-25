using System.Collections.Generic;
using UnityEngine;
using HorseFarm.Core;

namespace HorseFarm.Economy
{
    public static class InventorySystem
    {
        public static int GetItemCount(PlayerState state, string itemId)
        {
            if (state?.inventory?.items == null) return 0;
            int total = 0;
            for (int i = 0; i < state.inventory.items.Count; i++)
            {
                if (state.inventory.items[i].itemId == itemId)
                    total += state.inventory.items[i].count;
            }
            return total;
        }

        public static int GetUsedSlots(PlayerState state)
        {
            if (state?.inventory?.items == null) return 0;
            int used = 0;
            for (int i = 0; i < state.inventory.items.Count; i++)
            {
                if (state.inventory.items[i].count > 0)
                    used++;
            }
            return used;
        }

        public static bool CanAddItem(PlayerState state, string itemId, int count, int maxStackSize = 20)
        {
            if (state == null || count <= 0) return false;
            int remaining = count;

            for (int i = 0; i < state.inventory.items.Count; i++)
            {
                var stack = state.inventory.items[i];
                if (stack.itemId == itemId && stack.count < maxStackSize)
                {
                    int space = maxStackSize - stack.count;
                    remaining -= Mathf.Min(remaining, space);
                    if (remaining <= 0) return true;
                }
            }

            int freeSlots = state.inventory.slots - GetUsedSlots(state);
            int neededSlots = Mathf.CeilToInt(remaining / (float)maxStackSize);
            return freeSlots >= neededSlots;
        }

        public static bool AddItem(PlayerState state, string itemId, int count, int maxStackSize = 20)
        {
            if (!CanAddItem(state, itemId, count, maxStackSize))
                return false;

            int remaining = count;
            for (int i = 0; i < state.inventory.items.Count && remaining > 0; i++)
            {
                var stack = state.inventory.items[i];
                if (stack.itemId == itemId && stack.count < maxStackSize)
                {
                    int add = Mathf.Min(remaining, maxStackSize - stack.count);
                    stack.count += add;
                    remaining -= add;
                }
            }

            while (remaining > 0)
            {
                int add = Mathf.Min(remaining, maxStackSize);
                state.inventory.items.Add(new ItemStack { itemId = itemId, count = add });
                remaining -= add;
            }

            GameEvents.RaiseInventoryChanged();
            return true;
        }

        public static bool RemoveItem(PlayerState state, string itemId, int count)
        {
            if (GetItemCount(state, itemId) < count)
                return false;

            int remaining = count;
            for (int i = state.inventory.items.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var stack = state.inventory.items[i];
                if (stack.itemId == itemId)
                {
                    int take = Mathf.Min(remaining, stack.count);
                    stack.count -= take;
                    remaining -= take;
                    if (stack.count <= 0)
                        state.inventory.items.RemoveAt(i);
                }
            }

            GameEvents.RaiseInventoryChanged();
            return true;
        }
    }
}
