using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using HarmonyLib;

namespace TDBug
{
	/*
	 *Y U NO WORK
	[HarmonyPatch(typeof(DebugActionAttribute), MethodType.Constructor)]
	static class DebugMenuNoSecondPage
	{
		public static void Prefix(ref bool hideInSubMenu)
		{
			hideInSubMenu = false;
		}
	}
	*/

	[HarmonyPatch(typeof(DebugTabMenu_Actions), "GenerateCacheForMethod")]
	public static class DebugMenuNoSecondPage
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo hideInfo = AccessTools.Field(typeof(DebugActionAttribute), nameof(DebugActionAttribute.hideInSubMenu));
			foreach(var inst in instructions)
			{
				yield return inst;
				if(inst.LoadsField(hideInfo))
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return new CodeInstruction(OpCodes.Ldc_I4_0); //false
				}
			}
		}
	}
}
