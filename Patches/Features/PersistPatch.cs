using System.Collections.Generic;
using System.Reflection;
using BaseLib.Cards.Variables;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Features;


[HarmonyPatch(typeof(CardModel))]
public static class PersistPatch
{
    static MethodBase TargetMethod()
    {
        var targetMethod = AccessTools.DeclaredMethod(typeof(CardModel), "GetResultPileTypeForCardPlay");
        if (targetMethod == null)
            targetMethod = AccessTools.DeclaredMethod(typeof(CardModel), "GetResultPileType");

        return targetMethod;
    }
    
    [HarmonyTranspiler]
    static List<CodeInstruction> AltDestination(IEnumerable<CodeInstruction> instructions)
    {
        return new InstructionPatcher(instructions)
           .Match(new InstructionMatcher()
               .ldc_i4_4()
               .ret()
               .ldc_i4_3()
           )
           .Insert([
               CodeInstruction.LoadArgument(0),
               CodeInstruction.Call(typeof(PersistPatch), nameof(NormalOrPersist)),
            ]);
    }

    //patched to be lower priority than exhaust
    static PileType NormalOrPersist(PileType dest, CardModel model)
    {
        if (dest == PileType.Discard && IsPersist(model))
        {
            return PileType.Hand;
        }
        return dest;
    }
    
    public static bool IsPersist(this CardModel card)
    {
        var persistAmount = card.DynamicVars.TryGetValue(PersistVar.Key, out var val) ? val.IntValue : 0;
        return PersistVar.PersistCount(card, persistAmount) > 0;
    }
}
