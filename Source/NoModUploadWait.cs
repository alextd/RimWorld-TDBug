using Verse;
using Harmony;

namespace TDBug
{
	[HarmonyPatch(typeof(Dialog_MessageBox), "InteractionDelayExpired", MethodType.Getter)]
	class NoModUploadWait
	{
		public static bool Postfix(bool __result) => true;
	}
}
