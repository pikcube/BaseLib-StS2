using System.Collections.ObjectModel;
using BaseLib.Extensions;
using BaseLib.Patches.Localization;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

//TODO - save/load
/// <summary>
/// A model that is attached to a card to modify its behavior.
/// Receives all combat hooks, and is capable of modifying the card's description.
/// More features to be added in the future.
/// </summary>
public abstract class CardModifier : AbstractModel
{
    private static readonly SpireField<CardModel, List<CardModifier>> _modifiers = new(() => []);
    
    /// <summary>
    /// Gets the list of modifiers on a card.
    /// This list is read-only and cannot be modified.
    /// </summary>
    public static ReadOnlyCollection<CardModifier> Modifiers(CardModel card) => _modifiers[card]?.AsReadOnly() ?? throw new Exception("Card modifiers not found");
    /// <summary>
    /// Gets the list of modifiers on a card.
    /// Modifying this list will affect the modifiers on the card.
    /// It is not recommended to modify this list directly.
    /// </summary>
    public static List<CardModifier> DirectModifiers(CardModel card) => _modifiers[card] ?? throw new Exception("Card modifiers not found");

    /// <summary>
    /// Adds a card modifier to a card.
    /// </summary>
    public static void AddModifier(CardModel card, CardModifier modifier)
    {
        DirectModifiers(card).Add(modifier);
        modifier.Owner = card;
    }

    public static bool RemoveModifier(CardModel card, CardModifier modifier)
    {
        if (modifier.Owner == card)
            modifier.Owner = null;
        return DirectModifiers(card).Remove(modifier);
    }
    
    static CardModifier()
    {
        ModHelper.SubscribeForCombatStateHooks("BaseLibCardModifiers",
            combatState =>
            {
                List<CardModifier> modifiers = [];
                foreach (var piles in combatState.Players.Select(p => p.PlayerCombatState?.AllPiles))
                {
                    if (piles == null) continue;
                    foreach (var pile in piles)
                    {
                        foreach (var card in pile.Cards)
                        {
                            modifiers.AddRange(DirectModifiers(card));
                        }
                    }
                }

                return modifiers;
            });

        DescriptionOverrides.CustomizeDescription += (CardModel card, Creature? target, ref string description) =>
        {
            foreach (var modifier in DirectModifiers(card))
            {
                modifier.ModifyDescription(target, ref description);
            }
        };
        DescriptionOverrides.CustomizeDescriptionPost += (CardModel card, Creature? target, ref string description) =>
        {
            foreach (var modifier in DirectModifiers(card))
            {
                modifier.ModifyDescriptionPost(target, ref description);
            }
        };
    }

    public CardModel? Owner
    {
        get; 
        private set;
    }

    /// <summary>
    /// Modifies a card's description before the game processes it.
    /// Receives target passed into CardModel.GetDescriptionForPile.
    /// </summary>
    public virtual void ModifyDescription(Creature? target, ref string description)
    {
        
    }

    /// <summary>
    /// Modifies a card's description after the game processes it.
    /// Receives target passed into CardModel.GetDescriptionForPile.
    /// </summary>
    public virtual void ModifyDescriptionPost(Creature? target, ref string description)
    {
        
    }

    /// <summary>
    /// Called after the card modifier is cloned and added to a clone of a card.
    /// </summary>
    public virtual void AfterClonedOnCard(CardModel card)
    {
        
    }
}

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.MutableClone))]
static class CloneModifiers {
    [HarmonyPostfix]
    static void ModifyResult(AbstractModel __instance, AbstractModel __result)
    {
        if (__instance is CardModel card && __result is CardModel resultCard)
        {
            foreach (var modifier in CardModifier.Modifiers(card))
            {
                var cloneModifier = (CardModifier) modifier.MutableClone();
                resultCard.AddModifier(cloneModifier);
                cloneModifier.AfterClonedOnCard(resultCard);
            }
        }
    }
}