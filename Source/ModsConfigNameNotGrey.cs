using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace TDBug
{
	[HarmonyPatch(typeof(Page_ModsConfig), nameof(Page_ModsConfig.DoModRow))]
	public static class ModsConfigNameNotGrey
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo greyInfo = AccessTools.PropertyGetter(typeof(Color), nameof(Color.grey));
			MethodInfo whiteInfo = AccessTools.PropertyGetter(typeof(Color), nameof(Color.white));
			foreach (var inst in instructions)
			{
				if (inst.Calls(greyInfo))
					inst.operand = whiteInfo;
				yield return inst;
			}
		}
	}
}
