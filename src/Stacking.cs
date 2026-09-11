using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SpecializedStorage
{
    internal static class SpecializedStorageInventory
    {
        internal static void NormalizeExistingStacks(WorldGameObject chest)
        {
            StorageKind kind;
            if (!StorageRules.TryGetKind(chest, out kind)) return;
            Item data = chest.data;
            if (ReferenceEquals(data, null) || data.inventory == null || data.inventory.Count < 2) return;

            List<Item> inventory = data.inventory;

            for (int i = 0; i < inventory.Count; i++)
            {
                Item destination = inventory[i];
                if (ReferenceEquals(destination, null) || string.IsNullOrEmpty(destination.id) || destination.value <= 0 || ReferenceEquals(destination.definition, null)) continue;
                int vanilla = destination.definition.stack_count;
                if (vanilla <= 1 || !StorageRules.IsSuitable(kind, destination)) continue;

                int effective = StorageRules.EffectiveMax(vanilla);
                if (effective <= vanilla || destination.value >= effective) continue;

                int j = i + 1;
                while (j < inventory.Count && destination.value < effective)
                {
                    Item source = inventory[j];
                    if (ReferenceEquals(source, null) || source.id != destination.id || source.value <= 0)
                    {
                        j++;
                        continue;
                    }

                    int move = Math.Min(effective - destination.value, source.value);
                    destination.value += move;
                    source.value -= move;
                    if (source.value <= 0) inventory.RemoveAt(j); else j++;
                }
            }
        }
    }

    internal static class StackLimitOverride
    {
        internal static readonly FieldInfo ChestObjectField = AccessTools.Field(typeof(ChestGUI), "_chest_obj");

        private sealed class ActiveOverride
        {
            internal int Original;
            internal int Depth;
        }

        internal sealed class State
        {
            internal ItemDefinition Definition;
            internal bool Active;
        }

        private static readonly Dictionary<ItemDefinition, ActiveOverride> Active = new Dictionary<ItemDefinition, ActiveOverride>();

        internal static int GetVanillaStackCount(ItemDefinition definition)
        {
            if (ReferenceEquals(definition, null)) return 0;

            ActiveOverride existing;
            return Active.TryGetValue(definition, out existing)
                ? existing.Original
                : definition.stack_count;
        }

        internal static bool TryGetKind(ChestGUI gui, out StorageKind kind)
        {
            kind = StorageKind.None;
            if (ReferenceEquals(gui, null) || ChestObjectField == null) return false;
            return StorageRules.TryGetKind(ChestObjectField.GetValue(gui) as WorldGameObject, out kind);
        }

        internal static State TryApply(ChestGUI gui, Item item, bool toChest)
        {
            StorageKind kind;
            if (!toChest || ReferenceEquals(item, null) || !TryGetKind(gui, out kind)) return null;
            return TryApplyDefinition(kind, item);
        }

        internal static State TryApplyDefinition(StorageKind kind, Item item)
        {
            if (ReferenceEquals(item, null) || ReferenceEquals(item.definition, null)) return null;

            ItemDefinition definition = item.definition;
            string reason;
            if (!StorageRules.IsSuitable(kind, definition, item.id, out reason)) return null;

            ActiveOverride existing;
            if (Active.TryGetValue(definition, out existing))
            {
                existing.Depth++;
                return new State { Definition = definition, Active = true };
            }

            int original = definition.stack_count;
            int effective = StorageRules.EffectiveMax(original);
            if (effective <= original) return null;

            Active.Add(definition, new ActiveOverride { Original = original, Depth = 1 });
            definition.stack_count = effective;
            return new State { Definition = definition, Active = true };
        }

        internal static void Restore(State state)
        {
            if (state == null || !state.Active || ReferenceEquals(state.Definition, null)) return;

            ActiveOverride existing;
            if (!Active.TryGetValue(state.Definition, out existing))
            {
                state.Active = false;
                return;
            }

            existing.Depth--;
            if (existing.Depth <= 0)
            {
                state.Definition.stack_count = existing.Original;
                Active.Remove(state.Definition);
            }
            state.Active = false;
        }
    }
}
