using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;


namespace TDBug
{
	[HarmonyPatch(typeof(DebugActionAttribute), nameof(DebugActionAttribute.IsAllowedInCurrentGameState), MethodType.Getter)]
	public static class DebugNoExpansion
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo requiresRoyaltyInfo = AccessTools.Field(typeof(DebugActionAttribute), nameof(DebugActionAttribute.requiresRoyalty));
			FieldInfo requiresIdeologyInfo = AccessTools.Field(typeof(DebugActionAttribute), nameof(DebugActionAttribute.requiresIdeology));

			foreach (var inst in instructions)
			{
				if (inst.LoadsField(requiresIdeologyInfo) || inst.LoadsField(requiresRoyaltyInfo))
				{
					//this.requiresIdeology -> false
					yield return new CodeInstruction(OpCodes.Pop);//remove `this`
					yield return new CodeInstruction(OpCodes.Ldc_I4_0);//false
				}
				else
					yield return inst;
			}
		}
	}
}
