using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Hooks;
using BaseLib.Utils;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace BaseLib.Patches.Hooks;

/// <summary>
/// IHealAmountModifier.ModifyHealAdditive() -> IHealAmountModifier.ModifyHealMultiplicative()
/// </summary>
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal), MethodType.Async)]
public static class ModifyHealAmountPatches
{
//amount = Hook.ModifyHealAmount(creature.Player?.RunState ?? creature.CombatState?.RunState ?? NullRunState.Instance, creature.CombatState, creature, amount);
    [HarmonyTranspiler]
    static List<CodeInstruction> Patch(IEnumerable<CodeInstruction> code)
    {
        return new InstructionPatcher(code)
            .Match(new InstructionMatcher()
                .ldarg_0()
                .ldfld(null)
                .ldfld(null).PredicateMatch(op => op is FieldInfo field && field.Name.Contains("creature"))
            )
            .CopyMatch(out var loadCreature)
            .Match(new InstructionMatcher()
                .ldfld(null).PredicateMatch(op => op is FieldInfo field && field.Name.Equals("amount"))
            )
            .Step(-1)
            .GetOperand(out var amountField)
            .Insert(CodeInstruction.LoadArgument(0)) //Load arg 0 for the stfld to amountField later
            .Step(1)
            .Insert(loadCreature)
            .Insert([
                //Stack is statemachine - amount - creature
                CodeInstruction.Call(typeof(ModifyHealAmountPatches), nameof(ModifyHealAmountPatches.ModifyHeal)),
                //Stack is statemachine - amount
                new CodeInstruction(OpCodes.Stfld, amountField), //Store in statemachine amount field
                CodeInstruction.LoadArgument(0),
                new CodeInstruction(OpCodes.Ldfld, amountField)
            ]);
    }

    public static decimal ModifyHeal(decimal amount, Creature creature)
    {
        var combatState = creature.CombatState;
        if (combatState == null) return amount;
        
        amount = ModifyAdditive(combatState, creature, amount);
        amount = ModifyMultiplicative(combatState, creature, amount);
        
        return amount;
    }
    
    static decimal ModifyAdditive(ICombatState combatState, Creature creature, decimal amount)
    {
        // Aggregates modifications sequentially using the HookUtils pipeline
        return HookUtils.Aggregate<IHealAmountModifier, decimal>(
            combatState, 
            amount, 
            (mod, currentAcc) => currentAcc + mod.ModifyHealAdditive(creature, amount)
        );
    }

    static decimal ModifyMultiplicative(ICombatState combatState, Creature creature, decimal amount)
    {
        // Accumulates multiplicative modifiers and clamps the total floor to 0
        var finalResult = HookUtils.Aggregate<IHealAmountModifier, decimal>(
            combatState, 
            amount, 
            (mod, currentAcc) => currentAcc * mod.ModifyHealMultiplicative(creature, amount) 
        );

        return Math.Max(0m, finalResult);
    }
}
