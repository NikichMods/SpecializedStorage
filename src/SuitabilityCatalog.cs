using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace SpecializedStorage
{
    internal static class SuitabilityCatalog
    {
        private static readonly HashSet<string> AlchemyRecipeItems = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> KitchenRecipeItems = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, ItemDefinition> Definitions = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);

        private static readonly HashSet<string> KitchenStations = new HashSet<string>(StringComparer.Ordinal)
        {
            "cooking_table", "cooking_table_2", "oven", "cooking_bonfire",
            "tavern_kitchen", "tavern_oven",
            "refugee_camp_cooking_table", "refugee_camp_cooking_table_2"
        };

        private static bool _built;
        private static bool _building;
        private static bool _failed;

        internal static bool IsAlchemyRecipeItem(string itemId)
        {
            EnsureBuilt();
            return !_failed && MatchesFamily(AlchemyRecipeItems, itemId);
        }

        internal static bool IsKitchenRecipeItem(string itemId)
        {
            EnsureBuilt();
            return !_failed && MatchesFamily(KitchenRecipeItems, itemId);
        }

        private static void EnsureBuilt()
        {
            if (_built || _building || _failed) return;
            if (GameBalance.me == null || GameBalance.me.craft_data == null) return;

            _building = true;
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                Definitions.Clear();
                AlchemyRecipeItems.Clear();
                KitchenRecipeItems.Clear();

                BuildDefinitionMap();
                BuildAlchemySet();
                BuildKitchenSet();

                stopwatch.Stop();
                _built = true;
                SpecializedStoragePlugin.ModLog.LogInfo(
                    "Suitability cache built: definitions=" + Definitions.Count +
                    ", alchemy=" + AlchemyRecipeItems.Count +
                    ", kitchen=" + KitchenRecipeItems.Count +
                    ", crafts=" + GameBalance.me.craft_data.Count +
                    ", time_ms=" + stopwatch.ElapsedMilliseconds + ".");
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                _failed = true;
                AlchemyRecipeItems.Clear();
                KitchenRecipeItems.Clear();
                SpecializedStoragePlugin.ModLog.LogError("Suitability cache failed: " + exception);
            }
            finally
            {
                _building = false;
            }
        }

        private static void BuildAlchemySet()
        {
            foreach (CraftDefinition craft in GameBalance.me.craft_data)
            {
                if (craft == null) continue;
                if (craft.craft_type != CraftDefinition.CraftType.AlchemyDecompose &&
                    craft.craft_type != CraftDefinition.CraftType.MixedCraft) continue;

                AddItems(AlchemyRecipeItems, craft.needs, false);
                AddItems(AlchemyRecipeItems, craft.output, false);
            }
        }

        private static void BuildKitchenSet()
        {
            foreach (KeyValuePair<string, ItemDefinition> pair in Definitions)
            {
                if (HasBagType(pair.Value, ItemDefinition.BagType.Food))
                    KitchenRecipeItems.Add(pair.Key);
            }

            bool changed;
            int guard = 0;
            do
            {
                changed = false;
                guard++;

                foreach (CraftDefinition craft in GameBalance.me.craft_data)
                {
                    if (craft == null || !IsKitchenCraft(craft) || craft.output == null || craft.output.Count == 0) continue;
                    if (!AnyMatches(KitchenRecipeItems, craft.output)) continue;

                    // Deliberately walk only backwards through ingredients. A cooking recipe may
                    // have non-food side outputs (for example gold/silver nuggets from special fish).
                    changed |= AddItems(KitchenRecipeItems, craft.needs, true);
                }
            }
            while (changed && guard < 20);
        }

        private static bool IsKitchenCraft(CraftDefinition craft)
        {
            if (craft == null || craft.craft_in == null) return false;
            foreach (string station in craft.craft_in)
                if (!string.IsNullOrEmpty(station) && KitchenStations.Contains(station)) return true;
            return false;
        }

        private static bool AnyMatches(HashSet<string> set, List<Item> items)
        {
            if (items == null) return false;
            foreach (Item item in items)
            {
                if (item == null) continue;
                if (!string.IsNullOrEmpty(item.id) && MatchesFamily(set, item.id)) return true;
                if (item.multiquality_items == null) continue;
                foreach (string variant in item.multiquality_items)
                    if (!string.IsNullOrEmpty(variant) && MatchesFamily(set, variant)) return true;
            }
            return false;
        }

        private static bool AddItems(HashSet<string> set, List<Item> items, bool kitchenFilter)
        {
            bool changed = false;
            if (items == null) return false;

            foreach (Item item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.id) || IsPseudoResource(item.id)) continue;
                if (kitchenFilter && IsKitchenFalsePositive(item)) continue;

                changed |= set.Add(item.id);
                if (item.multiquality_items != null)
                {
                    foreach (string variant in item.multiquality_items)
                        if (!string.IsNullOrEmpty(variant)) changed |= set.Add(variant);
                }
            }
            return changed;
        }

        private static bool IsKitchenFalsePositive(Item item)
        {
            if (item == null || string.IsNullOrEmpty(item.id)) return true;
            string id = item.id;

            if (id == "stamp" || id == "paper_clean" || id == "skroll_skin_pig" || id == "ink:ink_jar" ||
                id == "pen_1" || id == "pen:ink_pen" || id == "note_wasted" || id == "pail_wet_paper") return true;

            if (Starts(id, "story:") || Starts(id, "notes:") || Starts(id, "chapter:") || Starts(id, "flyer:") ||
                Starts(id, "scroll_") || Starts(id, "skroll_") || Starts(id, "cover:") || Starts(id, "book:")) return true;

            return item.definition != null && item.definition.type == ItemDefinition.ItemType.Preach;
        }

        private static bool MatchesFamily(HashSet<string> set, string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            if (set.Contains(itemId)) return true;

            int index = itemId.LastIndexOf(':');
            while (index > 0)
            {
                string parent = itemId.Substring(0, index);
                if (set.Contains(parent)) return true;
                index = parent.LastIndexOf(':');
            }
            return false;
        }

        private static bool HasBagType(ItemDefinition definition, ItemDefinition.BagType expected)
        {
            return definition != null && definition.can_be_inserted_in_bag != null && definition.can_be_inserted_in_bag.Contains(expected);
        }

        private static bool Starts(string value, string prefix)
        {
            return value != null && value.StartsWith(prefix, StringComparison.Ordinal);
        }

        private static bool IsPseudoResource(string id)
        {
            return id == "g" || id == "r" || id == "b" || id == "money" || id == "faith" || id == "science" ||
                   id == "snack" || id == "meal" || id == "dessert";
        }

        private static void BuildDefinitionMap()
        {
            HashSet<ItemDefinition> definitions = new HashSet<ItemDefinition>();
            HashSet<object> visited = new HashSet<object>(ReferenceComparer.Instance);
            Assembly gameAssembly = typeof(ItemDefinition).Assembly;

            foreach (Type type in GetLoadableTypes(gameAssembly))
            {
                if (type == null || type.FullName == null || type.FullName.IndexOf("GameBalance", StringComparison.OrdinalIgnoreCase) < 0) continue;
                ScanStaticMembers(type, definitions, visited);
            }

            foreach (ItemDefinition definition in definitions)
            {
                string id = GetDefinitionId(definition);
                if (!string.IsNullOrEmpty(id) && id != "<null>" && !Definitions.ContainsKey(id))
                    Definitions.Add(id, definition);
            }
        }

        private static void ScanStaticMembers(Type type, HashSet<ItemDefinition> definitions, HashSet<object> visited)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (FieldInfo field in type.GetFields(flags))
            {
                try { Collect(field.GetValue(null), definitions, visited, 0); }
                catch { }
            }

            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                try
                {
                    if (property.GetIndexParameters().Length != 0) continue;
                    MethodInfo getter = property.GetGetMethod(true);
                    if (getter == null || !getter.IsStatic) continue;
                    Collect(property.GetValue(null, null), definitions, visited, 0);
                }
                catch { }
            }
        }

        private static void Collect(object value, HashSet<ItemDefinition> definitions, HashSet<object> visited, int depth)
        {
            if (value == null || depth > 5) return;

            ItemDefinition definition = value as ItemDefinition;
            if (definition != null)
            {
                definitions.Add(definition);
                return;
            }

            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string || value is decimal) return;
            if (!type.IsValueType && !visited.Add(value)) return;

            IDictionary dictionary = value as IDictionary;
            if (dictionary != null)
            {
                int n = 0;
                foreach (DictionaryEntry entry in dictionary)
                {
                    Collect(entry.Key, definitions, visited, depth + 1);
                    Collect(entry.Value, definitions, visited, depth + 1);
                    if (++n > 10000) break;
                }
                return;
            }

            IEnumerable enumerable = value as IEnumerable;
            if (enumerable != null)
            {
                int n = 0;
                foreach (object element in enumerable)
                {
                    Collect(element, definitions, visited, depth + 1);
                    if (++n > 10000) break;
                }
                return;
            }

            if (type.FullName == null || type.FullName.IndexOf("GameBalance", StringComparison.OrdinalIgnoreCase) < 0) return;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (FieldInfo field in type.GetFields(flags))
            {
                try { Collect(field.GetValue(value), definitions, visited, depth + 1); }
                catch { }
            }

            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                try
                {
                    if (property.GetIndexParameters().Length != 0) continue;
                    MethodInfo getter = property.GetGetMethod(true);
                    if (getter == null || getter.IsStatic) continue;
                    Collect(property.GetValue(value, null), definitions, visited, depth + 1);
                }
                catch { }
            }
        }

        private static string GetDefinitionId(ItemDefinition definition)
        {
            if (definition == null) return null;
            string[] names = { "id", "ID", "_id", "item_id", "itemId" };
            Type type = definition.GetType();

            foreach (string name in names)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(string))
                {
                    string value = field.GetValue(definition) as string;
                    if (!string.IsNullOrEmpty(value)) return value;
                }

                PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null && property.PropertyType == typeof(string) && property.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        string value = property.GetValue(definition, null) as string;
                        if (!string.IsNullOrEmpty(value)) return value;
                    }
                    catch { }
                }
            }
            return null;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException exception) { return exception.Types; }
            catch { return new Type[0]; }
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            internal static readonly ReferenceComparer Instance = new ReferenceComparer();
            public new bool Equals(object x, object y) { return ReferenceEquals(x, y); }
            public int GetHashCode(object obj) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj); }
        }
    }
}
