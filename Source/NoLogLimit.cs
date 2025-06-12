using Verse;
using HarmonyLib;

namespace TDBug
{
	[HarmonyPatch(typeof(Log), nameof(Log.PreventLogging), MethodType.Getter)]
	class NoLogLimit
	{
		public static bool Prefix(ref bool __result)
		{
			__result = Log.logDisablers > 0; // skip reachedMaxMessagesLimit check
			return false;
		}
	}
}
