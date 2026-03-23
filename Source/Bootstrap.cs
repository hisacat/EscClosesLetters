using HarmonyLib;
using Verse;

namespace HisaCat.EscClosesLetters
{
    [StaticConstructorOnStartup]
    internal static class Bootstrap
    {
        public const string id = "cat.hisa.escclosesletters";
        static Bootstrap()
        {
            var harmony = new Harmony(id);
            harmony.PatchAll();
        }
    }
}
