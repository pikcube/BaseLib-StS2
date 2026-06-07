using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace BaseLib.Patches.Content;

[HarmonyPatch(typeof(TrashHeap), nameof(TrashHeap.Relics), MethodType.Getter)]
class TrashHeapRelicsPatch
{
    private static RelicModel[]? _customRelics;

    [HarmonyPostfix]
    static void AddCustomRelics(ref RelicModel[] __result)
    {
        _customRelics ??= ModelDb.AllRelics.Where(relic => relic is ITrashHeapRelic).ToArray();
        if (_customRelics.Length == 0)
        {
            return;
        }

        __result =
        [
            ..__result,
            .._customRelics
        ];
    }
}

[HarmonyPatch(typeof(TrashHeap), nameof(TrashHeap.Cards), MethodType.Getter)]
class TrashHeapCardsPatch
{
    private static CardModel[]? _customCards;

    [HarmonyPostfix]
    static void AddCustomCards(ref CardModel[] __result)
    {
        _customCards ??= ModelDb.AllCards.Where(card => card is ITrashHeapCard).ToArray();
        if (_customCards.Length == 0)
        {
            return;
        }
        
        __result =
        [
            ..__result,
            .._customCards
        ];
    }
}