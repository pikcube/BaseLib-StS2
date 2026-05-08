using System.Reflection;
using BaseLib.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Features;

[HarmonyPatch(typeof(CardModel))]
public class PurgePatch
{
    static MethodBase TargetMethod()
    {
        var targetMethod = AccessTools.DeclaredMethod(typeof(CardModel), "GetResultPileTypeForCardPlay");
        if (targetMethod == null)
            targetMethod = AccessTools.DeclaredMethod(typeof(CardModel), "GetResultPileType");

        return targetMethod;
    }
    
    [HarmonyPrefix]
    static bool GoAwayForever(CardModel __instance, ref PileType __result)
    {
        if (ShouldPurge(__instance))
        {
            __result = PileType.None;
            return false;
        }

        return true;
    }

    public static bool ShouldPurge(CardModel c)
    {
        return c.Keywords.Contains(BaseLibKeywords.Purge);
    }
}