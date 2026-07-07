using BaseLib.Config;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BaseLib.Patches.Localization;

/// <summary>
/// Monsters have no own text hover tip, so for modded monsters the source mod is shown directly
/// under the monster's name on its nameplate.
/// </summary>
[HarmonyPatch(typeof(NCreatureStateDisplay), "RefreshValues")]
public static class MonsterSourceLabel
{
    [HarmonyPostfix]
    static void Postfix(NCreatureStateDisplay __instance)
    {
        if (!BaseLibConfig.ShowModSourceTooltip) return;

        var creature = Traverse.Create(__instance).Field("_creature").GetValue<Creature>();
        var monster = creature?.Monster;
        if (monster == null) return;

        var name = WhatMod.FindModName(monster.GetType());
        if (name == null) return;

        var label = Traverse.Create(__instance).Field("_nameplateLabel").GetValue<MegaLabel>();
        if (label == null) return;

        var title = ModSourceTooltip.TitleLoc().GetFormattedText();
        label.SetTextAutoSize($"{creature.Name}\n{title}: {name}");
    }
}
