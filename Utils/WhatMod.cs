using System.Reflection;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Modding;

namespace BaseLib.Utils;

/// <summary>
/// Maps modded content back to the mod that added it, by the assembly its type was defined in,
/// so tooltips can show "which mod is this from?".
/// </summary>
public static class WhatMod
{
    private static readonly Dictionary<Assembly, Mod> ModByAssembly = new();
    private static readonly Dictionary<Type, Mod?> ModByType = new();
    private static IReadOnlyList<Mod> _loadedMods = [];
    private static bool _built;

    private static void BuildIfNeeded()
    {
        if (_built) return;
        _built = true;
        _loadedMods = ModManager.GetLoadedMods().ToList();
        foreach (var mod in _loadedMods)
        {
            if (mod.assembly == null) continue;
            ModByAssembly[mod.assembly] = mod;
        }
    }

    /// <summary>
    /// The mod that defined <paramref name="type"/>, or null if it belongs to the base game.
    /// </summary>
    public static Mod? FindMod(Type type)
    {
        if (ModByType.TryGetValue(type, out var cached)) return cached;

        BuildIfNeeded();
        if (!ModByAssembly.TryGetValue(type.Assembly, out var mod))
        {
            // Assembly not registered directly (e.g. bundled); fall back to matching the content's
            // root namespace against a loaded mod's id.
            var root = type.GetRootNamespace();
            mod = _loadedMods.FirstOrDefault(m =>
                m.manifest?.id != null && m.manifest.id.Equals(root, StringComparison.OrdinalIgnoreCase));
        }

        ModByType[type] = mod;
        return mod;
    }

    /// <summary>
    /// Display name of the mod that defined <paramref name="type"/>, or null if it is base-game content.
    /// Matches the installed-mods list: the manifest name, with the id in parentheses when it differs.
    /// </summary>
    public static string? FindModName(Type type)
    {
        var rootNamespace = type.GetRootNamespace();
        if (string.IsNullOrEmpty(rootNamespace) || rootNamespace.Equals("MegaCrit", StringComparison.Ordinal))
            return null;

        var mod = FindMod(type);
        var name = mod?.manifest?.name;
        var id = mod?.manifest?.id ?? rootNamespace;

        if (string.IsNullOrWhiteSpace(name))
            return id;
        if (string.IsNullOrWhiteSpace(id) || id.Equals(name, StringComparison.OrdinalIgnoreCase))
            return name;
        return $"{name} ({id})";
    }
}
