using Verse;
using HarmonyLib;

namespace TDBug
{
	[HarmonyPatch(typeof(Dialog_MessageBox), nameof(Dialog_MessageBox.InteractionDelayExpired), MethodType.Getter)]
	class NoModUploadWait
	{
		public static bool Postfix(bool __result) => true;
	}
}
