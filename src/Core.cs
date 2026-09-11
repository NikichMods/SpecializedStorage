using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace SpecializedStorage
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("pandamodding.gyk.quickstack", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class SpecializedStoragePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "nikich.graveyardkeeper.specializedstorage";
        public const string PluginName = "Specialized Storage";
        public const string PluginVersion = "1.2.0";

        internal static ManualLogSource ModLog;
        private Harmony _harmony;

        private void Awake()
        {
            ModLog = Logger;
            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(SpecializedStoragePlugin).Assembly);
            QuickStackCompatibility.TryPatch(_harmony);
        }

        private void OnDestroy()
        {
            StackMarkerUI.Dispose();
            if (_harmony != null) _harmony.UnpatchSelf();
        }
    }

    internal enum StorageKind
    {
        None = 0,
        Alchemy = 1,
        Kitchen = 2,
        ScrollShelf = 3,
        Bookcase = 4,
        Mortuary = 5
    }

    internal static class StorageRules
    {
        internal const int StackMultiplier = 4;
        internal const int StackCap = 200;

        private static readonly HashSet<string> KitchenWare = new HashSet<string>(StringComparer.Ordinal)
        {
            "bottle_empty_1", "bottle_empty_2", "bowl_empty", "cup_empty",
            "food_empty_plate_1", "jug_empty", "ceramic_1", "ceramic_2", "ceramic_3"
        };

        private static readonly HashSet<string> LoggedLegacyOverstacks = new HashSet<string>(StringComparer.Ordinal);

        internal static bool TryGetKind(WorldGameObject chest, out StorageKind kind)
        {
            kind = StorageKind.None;
            if (ReferenceEquals(chest, null)) return false;

            switch (chest.obj_id)
            {
                case "rack_alchemy": kind = StorageKind.Alchemy; return true;
                case "keeper_room_rack": kind = StorageKind.Kitchen; return true;
                case "obj_church_scroll_cabinet": kind = StorageKind.ScrollShelf; return true;
                case "obj_church_bookcase": kind = StorageKind.Bookcase; return true;
                case "rack_organs": kind = StorageKind.Mortuary; return true;
                default: return false;
            }
        }

        internal static string KindName(StorageKind kind)
        {
            switch (kind)
            {
                case StorageKind.Alchemy: return "rack_alchemy";
                case StorageKind.Kitchen: return "keeper_room_rack";
                case StorageKind.ScrollShelf: return "obj_church_scroll_cabinet";
                case StorageKind.Bookcase: return "obj_church_bookcase";
                case StorageKind.Mortuary: return "rack_organs";
                default: return "<none>";
            }
        }

        internal static int EffectiveMax(int vanillaMax)
        {
            if (vanillaMax <= 1 || vanillaMax >= StackCap) return vanillaMax;
            long scaled = (long)vanillaMax * StackMultiplier;
            return (int)Math.Min((long)StackCap, scaled);
        }

        internal static bool IsSuitable(StorageKind kind, Item item)
        {
            string ignored;
            return !ReferenceEquals(item, null) && IsSuitable(kind, item.definition, item.id, out ignored);
        }

        internal static bool IsSuitable(StorageKind kind, ItemDefinition definition, string itemId, out string reason)
        {
            reason = "no matching rule";
            if (ReferenceEquals(definition, null) || string.IsNullOrEmpty(itemId) || IsTestItem(itemId)) return false;

            if (kind == StorageKind.Alchemy)
            {
                if ((int)definition.alch_type != 0)
                {
                    reason = "alch_type=" + definition.alch_type;
                    return true;
                }
                if (HasBagType(definition, ItemDefinition.BagType.Alchemy))
                {
                    reason = "BagType.Alchemy";
                    return true;
                }
                if (HasBagType(definition, ItemDefinition.BagType.Potions))
                {
                    reason = "BagType.Potions";
                    return true;
                }
                if (SuitabilityCatalog.IsAlchemyRecipeItem(itemId))
                {
                    reason = "recipe graph: AlchemyDecompose/MixedCraft";
                    return true;
                }
                return false;
            }

            if (kind == StorageKind.Kitchen)
            {
                if (HasBagType(definition, ItemDefinition.BagType.Food))
                {
                    reason = "BagType.Food";
                    return true;
                }
                if (KitchenWare.Contains(itemId))
                {
                    reason = "kitchenware";
                    return true;
                }
                if (SuitabilityCatalog.IsKitchenRecipeItem(itemId))
                {
                    reason = "recipe graph: culinary inputs";
                    return true;
                }
                return false;
            }

            if (kind == StorageKind.ScrollShelf)
            {
                if (definition.type == ItemDefinition.ItemType.Preach)
                {
                    reason = "ItemType.Preach";
                    return true;
                }

                if (itemId == "paper_clean" || itemId == "skroll_skin_pig" || itemId == "ink:ink_jar" ||
                    itemId == "pen_1" || itemId == "pen:ink_pen" || itemId == "note_wasted" ||
                    itemId == "teleport_scroll")
                {
                    reason = "writing material/tool";
                    return true;
                }

                if (Starts(itemId, "story:") || Starts(itemId, "notes:") || Starts(itemId, "flyer:") ||
                    Starts(itemId, "preach_") || Starts(itemId, "scroll_") || Starts(itemId, "skroll_"))
                {
                    reason = "writing/scroll family";
                    return true;
                }
                return false;
            }

            if (kind == StorageKind.Bookcase)
            {
                if (Starts(itemId, "chapter:") || Starts(itemId, "cover:") || Starts(itemId, "book:") ||
                    Starts(itemId, "book_soft:") || Starts(itemId, "techbook_"))
                {
                    reason = "book production/library family";
                    return true;
                }

                if (itemId == "book_ruined_1" || itemId == "endless_book" || itemId == "book_of_receipts" ||
                    itemId == "quest_cultit_book" || itemId == "lost_book_part")
                {
                    reason = "book/library item";
                    return true;
                }
                return false;
            }

            if (kind == StorageKind.Mortuary)
            {
                if (definition.type == ItemDefinition.ItemType.BodyUniversalPart)
                {
                    reason = "ItemType.BodyUniversalPart";
                    return true;
                }

                if (Starts(itemId, "embalm_"))
                {
                    reason = "embalming consumable";
                    return true;
                }
                return false;
            }

            return false;
        }

        internal static void CheckLegacyOverstacks(WorldGameObject chest)
        {
            StorageKind kind;
            if (!TryGetKind(chest, out kind)) return;
            Item data = chest.data;
            if (ReferenceEquals(data, null) || data.inventory == null) return;

            foreach (Item item in data.inventory)
            {
                if (ReferenceEquals(item, null) || string.IsNullOrEmpty(item.id) || ReferenceEquals(item.definition, null)) continue;

                string reason;
                bool suitable = IsSuitable(kind, item.definition, item.id, out reason);
                if (!suitable && item.definition.stack_count > 0 && item.value > item.definition.stack_count)
                {
                    string overKey = KindName(kind) + ":" + item.id + ":" + item.value + ":" + item.definition.stack_count;
                    if (LoggedLegacyOverstacks.Add(overKey))
                    {
                        SpecializedStoragePlugin.ModLog.LogWarning(
                            "Legacy overstack left unchanged for non-specialized item in " + KindName(kind) + ": " + item.id +
                            " value=" + item.value + ", vanilla_max=" + item.definition.stack_count + ".");
                    }
                }
            }
        }

        private static bool HasBagType(ItemDefinition definition, ItemDefinition.BagType expected)
        {
            return definition != null && definition.can_be_inserted_in_bag != null && definition.can_be_inserted_in_bag.Contains(expected);
        }

        private static bool Starts(string value, string prefix)
        {
            return value != null && value.StartsWith(prefix, StringComparison.Ordinal);
        }

        private static bool IsTestItem(string id)
        {
            return Starts(id, "test_") || Starts(id, "test:");
        }
    }
}
