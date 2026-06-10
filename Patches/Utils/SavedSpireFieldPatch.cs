using BaseLib.Patches.Saves;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Patches.Utils;

[HarmonyPatch(typeof(SavedProperties))]
static class SavedSpireFieldPatch
{
    private static readonly List<ISavedSpireField> RegisteredFields = [];

    public static void Register<TKey, TVal>(SavedSpireField<TKey, TVal> field)
        where TKey : class => RegisteredFields.Add(field);

    private static IEnumerable<ISavedSpireField> GetFieldsForModel(object model) =>
        RegisteredFields.Where(f => f.TargetType.IsInstanceOfType(model));
    
    [HarmonyPatch(nameof(SavedProperties.FromInternal))]
    [HarmonyPostfix]
    static void PostfixFromInternal(ref SavedProperties? __result, object model)
    {
        var props = __result ?? new SavedProperties();
        bool added = false;
        foreach (var field in GetFieldsForModel(model))
        {
            field.Export(model, props);
            added = true;
        }
        if (__result == null && added)
            __result = props;
    }

    [HarmonyPatch(nameof(SavedProperties.FillInternal))]
    [HarmonyPostfix]
    static void PostfixFillInternal(SavedProperties __instance, object model)
    {
        foreach (var field in GetFieldsForModel(model))
            field.Import(model, __instance);
    }
    
    internal static void AddFieldsSorted()
    {
        BaseLibMain.Logger.Info($"Found {RegisteredFields.Count} SavedSpireFields.");
        RegisteredFields.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        foreach (var field in RegisteredFields)
        {
            if (field.IsBasegameSupported)
            {
                InjectNameIntoBaseGameCache(field.Name);
            }
            else if (!field.RegisterCustomSave())
            {
                BaseLibMain.Logger.Error($"SavedSpireField {field.Name} will not be saved as it is of an unsupported type.");
            }
        }
    }
    
    private static void InjectNameIntoBaseGameCache(string name)
    {
        var propertyToId = AccessTools.StaticFieldRefAccess<Dictionary<string, int>>(
            typeof(SavedPropertiesTypeCache),
            "_propertyNameToNetIdMap"
        );
        var idToProperty = AccessTools.StaticFieldRefAccess<List<string>>(
            typeof(SavedPropertiesTypeCache),
            "_netIdToPropertyNameMap"
        );

        if (!propertyToId.ContainsKey(name))
        {
            propertyToId[name] = idToProperty.Count;
            idToProperty.Add(name);
            
            BaseLibMain.Logger.Debug($"Added saved property name to basegame cache: {name} => {propertyToId[name]}");

            int newBitSize = Mathf.CeilToInt(Math.Log2(idToProperty.Count));

            AccessTools
                .Property(typeof(SavedPropertiesTypeCache), "NetIdBitSize")
                .SetValue(null, newBitSize);
        }
        else
        {
            BaseLibMain.Logger.Error($"SavedSpireField name is not unique: {name}");
        }
    }
}
