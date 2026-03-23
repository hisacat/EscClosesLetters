using System;
using HarmonyLib;
using HisaCat.EscClosesLetters.InternalRefs.Members;
using Verse;

namespace HisaCat.EscClosesLetters.InternalRefs.Accessors
{
    internal static class DiaOption_Extensions
    {
        private static readonly AccessTools.FieldRef<DiaOption, string> DiaOptionTextRef =
            AccessTools.FieldRefAccess<DiaOption, string>(DiaOption_Members.text);
        public static string GetText(this DiaOption option) => DiaOptionTextRef(option);

        private static readonly Action<DiaOption> ActivateRef =
            AccessTools.MethodDelegate<Action<DiaOption>>(
                AccessTools.Method(typeof(DiaOption), DiaOption_Members.Activate)
            );
        public static void Activate(this DiaOption option)
        {
            ActivateRef(option);
        }
    }

    internal static class Dialog_NodeTree_Extensions
    {
        private static readonly AccessTools.FieldRef<Dialog_NodeTree, DiaNode> curNodeRef =
            AccessTools.FieldRefAccess<Dialog_NodeTree, DiaNode>("curNode");
        public static DiaNode GetCurNode(this Dialog_NodeTree tree) => curNodeRef(tree);
    }
}
