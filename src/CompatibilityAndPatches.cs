using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace SpecializedStorage
{
    internal static class QuickStackCompatibility
    {
        private const string QuickStackPluginGuid = "pandamodding.gyk.quickstack";
        private static readonly string[] TargetMethodNames = { "TryQuickStack", "CanQuickStack", "CountPotentialMove" };

        private static int Depth;
        private static WorldGameObject ActiveChest;
        private static List<StackLimitOverride.State> ActiveStates;

        private sealed class ScopeState
        {
            internal bool Active;
        }

        internal static void TryPatch(Harmony harmony)
        {
            Type pluginType = FindQuickStackPluginType();
            if (pluginType == null) return;

            int patched = 0;
            MethodInfo prefix = AccessTools.Method(typeof(QuickStackCompatibility), nameof(Prefix));
            MethodInfo finalizer = AccessTools.Method(typeof(QuickStackCompatibility), nameof(Finalizer));

            foreach (Type type in GetLoadableTypes(pluginType.Assembly))
            {
                if (type == null) continue;
                MethodInfo[] methods;
                try { methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic); }
                catch { continue; }

                foreach (MethodInfo method in methods)
                {
                    if (!IsTargetName(method.Name)) continue;
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length != 1 || parameters[0].ParameterType != typeof(WorldGameObject)) continue;

                    harmony.Patch(method, prefix: new HarmonyMethod(prefix), finalizer: new HarmonyMethod(finalizer));
                    patched++;
                }
            }

            if (patched == 0)
                SpecializedStoragePlugin.ModLog.LogWarning("GYK Quick Stack was found, but no compatible methods were patched.");
        }

        private static void Prefix(WorldGameObject __0, out ScopeState __state)
        {
            __state = Enter(__0);
        }

        private static Exception Finalizer(Exception __exception, ScopeState __state)
        {
            Exit(__state);
            return __exception;
        }

        private static ScopeState Enter(WorldGameObject chest)
        {
            StorageKind kind;
            if (!StorageRules.TryGetKind(chest, out kind)) return null;

            if (Depth > 0)
            {
                if (!ReferenceEquals(ActiveChest, chest))
                {
                    SpecializedStoragePlugin.ModLog.LogWarning("Ignoring nested Quick Stack scope for a different specialized chest.");
                    return null;
                }

                Depth++;
                return new ScopeState { Active = true };
            }

            List<StackLimitOverride.State> states = new List<StackLimitOverride.State>();
            try
            {
                SpecializedStorageInventory.NormalizeExistingStacks(chest);
                StorageRules.CheckLegacyOverstacks(chest);

                Item data = chest.data;
                HashSet<ItemDefinition> seen = new HashSet<ItemDefinition>();
                if (!ReferenceEquals(data, null) && data.inventory != null)
                {
                    foreach (Item item in data.inventory)
                    {
                        if (ReferenceEquals(item, null) || ReferenceEquals(item.definition, null) || !seen.Add(item.definition)) continue;
                        StackLimitOverride.State state = StackLimitOverride.TryApplyDefinition(kind, item);
                        if (state != null && state.Active) states.Add(state);
                    }
                }

                ActiveChest = chest;
                ActiveStates = states;
                Depth = 1;
                return new ScopeState { Active = true };
            }
            catch (Exception exception)
            {
                for (int i = states.Count - 1; i >= 0; i--) StackLimitOverride.Restore(states[i]);
                ActiveChest = null;
                ActiveStates = null;
                Depth = 0;
                SpecializedStoragePlugin.ModLog.LogError("Failed to enter specialized Quick Stack scope: " + exception);
                return null;
            }
        }

        private static void Exit(ScopeState state)
        {
            if (state == null || !state.Active) return;
            if (Depth > 0) Depth--;

            if (Depth == 0)
            {
                if (ActiveStates != null)
                    for (int i = ActiveStates.Count - 1; i >= 0; i--) StackLimitOverride.Restore(ActiveStates[i]);

                ActiveStates = null;
                ActiveChest = null;
            }

            state.Active = false;
        }

        private static Type FindQuickStackPluginType()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in GetLoadableTypes(assembly))
                {
                    if (type == null) continue;
                    object[] attributes;
                    try { attributes = type.GetCustomAttributes(typeof(BepInPlugin), false); }
                    catch { continue; }

                    foreach (object obj in attributes)
                    {
                        BepInPlugin attribute = obj as BepInPlugin;
                        if (attribute != null && attribute.GUID == QuickStackPluginGuid) return type;
                    }
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

        private static bool IsTargetName(string name)
        {
            for (int i = 0; i < TargetMethodNames.Length; i++)
                if (TargetMethodNames[i] == name) return true;
            return false;
        }
    }

    [HarmonyPatch(typeof(ChestGUI), "Open", new Type[] { typeof(WorldGameObject) })]
    internal static class ChestOpenPatch
    {
        private static void Prefix(ChestGUI __instance, WorldGameObject __0)
        {
            StackMarkerUI.OnChestOpening(__instance, __0);

            StorageKind kind;
            if (!StorageRules.TryGetKind(__0, out kind)) return;

            try
            {
                SpecializedStorageInventory.NormalizeExistingStacks(__0);
                StorageRules.CheckLegacyOverstacks(__0);
            }
            catch (Exception exception)
            {
                SpecializedStoragePlugin.ModLog.LogError("Failed to normalize/classify " + StorageRules.KindName(kind) + " on open: " + exception);
            }
        }
    }

    [HarmonyPatch(typeof(ChestGUI), "Hide", new Type[] { typeof(bool) })]
    internal static class ChestHideStackMarkerPatch
    {
        private static void Postfix(ChestGUI __instance)
        {
            StackMarkerUI.OnChestClosed(__instance);
        }
    }

    [HarmonyPatch(typeof(BaseItemCellGUI), "DrawItem",
        new Type[] { typeof(Item), typeof(bool), typeof(string), typeof(bool), typeof(bool) })]
    internal static class ItemCellStackMarkerPatch
    {
        private static void Postfix(BaseItemCellGUI __instance, Item __0)
        {
            StackMarkerUI.Refresh(__instance, __0);
        }
    }

    [HarmonyPatch(typeof(ChestGUI), "GetMaxMoveCount", new Type[] { typeof(Item), typeof(bool), typeof(Item), typeof(bool) })]
    internal static class GetMaxMoveCountPatch
    {
        private static readonly HashSet<StorageKind> LoggedCapacityClamps = new HashSet<StorageKind>();

        private static void Prefix(ChestGUI __instance, Item __0, bool __1, out StackLimitOverride.State __state)
        {
            __state = StackLimitOverride.TryApply(__instance, __0, __1);
        }

        private static void Postfix(ChestGUI __instance, ref int __result)
        {
            StorageKind kind;
            if (__result < 0 && StackLimitOverride.TryGetKind(__instance, out kind))
            {
                if (LoggedCapacityClamps.Add(kind))
                {
                    SpecializedStoragePlugin.ModLog.LogWarning(
                        StorageRules.KindName(kind) + " reported negative vanilla capacity; clamped to 0 for legacy-overstack safety.");
                }
                __result = 0;
            }
        }

        private static Exception Finalizer(Exception __exception, StackLimitOverride.State __state)
        {
            StackLimitOverride.Restore(__state);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(ChestGUI), "MoveItem", new Type[] { typeof(Item), typeof(int), typeof(bool), typeof(Item), typeof(bool) })]
    internal static class MoveItemPatch
    {
        private static void Prefix(ChestGUI __instance, Item __0, bool __2, out StackLimitOverride.State __state)
        {
            __state = StackLimitOverride.TryApply(__instance, __0, __2);
        }

        private static Exception Finalizer(Exception __exception, StackLimitOverride.State __state)
        {
            StackLimitOverride.Restore(__state);
            return __exception;
        }
    }
}
