using HarmonyLib;
using Verse;
using UnityEngine;
using HisaCat.EscClosesLetters.InternalRefs.Members;
using HisaCat.EscClosesLetters.InternalRefs.Accessors;
using System.Reflection;
using System.Collections.Generic;
using HisaCat.Utilities;

namespace HisaCat.EscClosesLetters
{
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(LanguageDatabase), nameof(LanguageDatabase.InitAllMetadata))]
    internal static class Patch_LanguageDatabase_InitAllMetadata
    {
        // Language change reloads play data asynchronously:
        // SelectLanguage -> PlayDataLoader.LoadAllPlayData -> LanguageDatabase.InitAllMetadata.
        // Update dismiss option texts here so they reflect the newly loaded language metadata.
        static void Postfix()
        {
            LetterDismissConfig.UpdateDismissOptionTexts();
            Logger.Message(nameof(LanguageDatabase.InitAllMetadata), $"Dismiss option texts updated.");
        }
    }

    [HarmonyPatch]
    internal static class Patch_ChoiceLetter_AllDerived_OpenLetter
    {
        static IEnumerable<MethodBase> TargetMethods()
            => HarmonyUtilities.GetOverriddenMethods(typeof(ChoiceLetter), nameof(ChoiceLetter.OpenLetter), includeRootType: true);

        struct OpenLetterContext { public bool isLeafType; }

        [HarmonyPriority(Priority.Last)]
        static void Prefix(ChoiceLetter __instance, MethodBase __originalMethod, ref OpenLetterContext __state)
        {
            // To handle cases where a class inherits from Letter through two or more levels
            // and its OpenLetter implementation calls base.OpenLetter,
            // we ensure that the patch logic only runs for the actual leaf type of the instance.
            // Without this check, a single OpenLetter call on one object could trigger
            // multiple patch methods and cause unintended behavior.
            // * The vanilla game code does not contain such cases.
            // However, other mods may use this pattern, so this serves as a minimal
            // safeguard against that possibility.
            __state = new() { isLeafType = __instance.GetType() == __originalMethod.DeclaringType };

            LetterDismissContext.IsOpeningLetter = true;
            Logger.Message($"{nameof(ChoiceLetter.OpenLetter)}.{nameof(Prefix)}",
                $"Type: '{__instance.GetType().FullName}'");
        }

        static void Finalizer(ChoiceLetter __instance, ref OpenLetterContext __state)
        {
            if (__state.isLeafType == false) return;

            LetterDismissContext.IsOpeningLetter = false;
            Logger.Message($"{nameof(ChoiceLetter.OpenLetter)}.{nameof(Finalizer)}",
                $"Type: '{__instance.GetType().FullName}'");
        }
    }

    // Must run after other patches that modify the constructor, so we base our logic on the final dialog state.
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(Dialog_NodeTree), MethodType.Constructor,
        new[] { typeof(DiaNode), typeof(bool), typeof(bool), typeof(string) })]
    // (DiaNode nodeRoot, bool delayInteractivity = false, bool radioMode = false, string title = null)
    internal static class Patch_Dialog_NodeTree_Ctor
    {
#pragma warning disable IDE0051
        static void Postfix(Dialog_NodeTree __instance)
        {
            __instance.GetCurNode();
            if (LetterDismissContext.IsOpeningLetter == false) return;
            Logger.Message(nameof(Patch_Dialog_NodeTree_Ctor), $"Letter dialog constructed.");

            if (LetterDismissPolicy.CanDismissDialog(__instance, out var dismissOption) == false) return;
            Logger.Message(nameof(Patch_Dialog_NodeTree_Ctor), $"Dismiss option: '{dismissOption.GetText()}'");

            // We hook into OnCancelKeyPressed and OnAcceptKeyPressed, but they are only
            // called by WindowStack.Notify_PressedCancel / Notify_PressedAccept when
            // closeOnCancel / closeOnAccept is true. Therefore, we enable each flag here
            // according to the user's settings.

            // Also, while vanilla letters usually have both flags set to false, letters
            // added or modified by other mods may already have one or both flags set to
            // true, in which case we simply leave them as they are and do nothing.
            var settings = LetterDismissModSettings.Instance;
            if (__instance.closeOnCancel == false && settings.UseCancelKeyToDismiss)
            {
                LetterDismissContext.SetTarget(__instance, dismissOption);

                __instance.closeOnCancel = true;
                LetterDismissContext.SetCloseOnCancel(true);

            }
            if (__instance.closeOnAccept == false && settings.UseAcceptKeyToDismiss)
            {
                LetterDismissContext.SetTarget(__instance, dismissOption);

                __instance.closeOnAccept = true;
                LetterDismissContext.SetCloseOnAccept(true);
            }
        }
#pragma warning restore IDE0051
    }

    // Run before other patches so we can clear our dismiss target as soon as this option is activated.
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(DiaOption), nameof(DiaOption_Members.Activate))]
    internal static class Patch_DiaOption_Activate
    {
#pragma warning disable IDE0051
        static void Postfix(DiaOption __instance)
        {
            // We cannot rule out cases where another mod invokes this DiaOption's Activate method.
            // As a minimal safeguard against conflicts, clear the stored reference after the method
            // has been invoked so it will not be invoked again.
            if (LetterDismissContext.TargetDismissOption == __instance)
            {
                if (LetterDismissContext.TargetDialog != null)
                {
                    LetterDismissContext.ClearTarget();
                    Logger.Message(nameof(DiaOption_Members.Activate), $"Target letter dialog cleared (dismiss option activated).");
                }
            }
        }
#pragma warning restore IDE0051
    }

    internal static class Patch_Window
    {
        [HarmonyPatch(typeof(Window), nameof(Window.PreClose))]
        internal static class Patch_PreClose
        {
            static void Postfix(Window __instance)
            {
                if (LetterDismissContext.TargetDialog != __instance) return;

                if (LetterDismissContext.TargetDialog != null)
                {
                    LetterDismissContext.ClearTarget();
                    Logger.Message(nameof(Window.PreClose), $"Target letter dialog cleared (dialog closing).");
                }
            }
        }

        [HarmonyPatch(typeof(Window), nameof(Window.OnCancelKeyPressed))]
        internal static class Patch_OnCancelKeyPressed
        {
            static bool Prefix(Window __instance)
            {
                if (LetterDismissContext.TargetDialog != __instance) return true;
                if (LetterDismissContext.CloseOnCancel == false) return true;

                if (LetterDismissModSettings.Instance.UseCancelKeyToDismiss == false) return true;

                if (TryActivateDismissOption(nameof(Window.OnCancelKeyPressed)) == false) return true;

                Event.current.Use(); // Consume the current event.
                return false; // Skip original method.
            }
        }

        [HarmonyPatch(typeof(Window), nameof(Window.OnAcceptKeyPressed))]
        internal static class Patch_OnAcceptKeyPressed
        {
            static bool Prefix(Window __instance)
            {
                if (LetterDismissContext.TargetDialog != __instance) return true;
                if (LetterDismissContext.CloseOnAccept == false) return true;

                if (LetterDismissModSettings.Instance.UseAcceptKeyToDismiss == false) return true;

                if (TryActivateDismissOption(nameof(Window.OnAcceptKeyPressed)) == false) return true;

                Event.current.Use(); // Consume the current event.
                return false; // Skip original method.
            }
        }

        internal static bool TryActivateDismissOption(string methodName)
        {
            if (LetterDismissContext.TargetDismissOption == null) return false;

            Logger.Message(methodName, $"Use '{LetterDismissContext.TargetDismissOption.GetText()}'");
            LetterDismissContext.TargetDismissOption.Activate();
            LetterDismissContext.ClearTarget();
            return true;
        }
    }
}
