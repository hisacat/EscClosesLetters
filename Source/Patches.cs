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

        struct OpenLetterContext { public bool isLeafOverriddenMethodForThisCall; }

        [HarmonyPriority(Priority.Last)]
        static void Prefix(ChoiceLetter __instance, MethodBase __originalMethod, ref OpenLetterContext __state)
        {
            // If multiple OpenLetter overrides exist in the inheritance chain and a leaf override
            // calls base.OpenLetter(), Harmony patches on each override can all run for one logical call.
            // Restrict the opening-flag ownership to the leaf overridden method only,
            // so the flag is set/cleared exactly once for that call.
            // * The vanilla game code does not contain such cases.
            // However, other mods may use this pattern, so this serves as a minimal
            // safeguard against that possibility.
            __state = new() { isLeafOverriddenMethodForThisCall = __instance.GetType() == __originalMethod.DeclaringType };
            if (__state.isLeafOverriddenMethodForThisCall == false) return;

            LetterDismissContext.IsOpeningLetter = true;
            Logger.Message($"{nameof(ChoiceLetter.OpenLetter)}.{nameof(Prefix)}",
                $"Type: '{__instance.GetType().FullName}'");
        }

        static void Finalizer(ChoiceLetter __instance, ref OpenLetterContext __state)
        {
            if (__state.isLeafOverriddenMethodForThisCall == false) return;

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
            bool enableCloseOnCancel = __instance.closeOnCancel == false && settings.UseCancelKeyToDismiss;
            bool enableCloseOnAccept = __instance.closeOnAccept == false && settings.UseAcceptKeyToDismiss;
            bool useClickedOutsideDismiss = __instance.closeOnClickedOutside == false && settings.CloseOnClickedOutside;

            bool dismissOptionEnabled = enableCloseOnCancel || enableCloseOnAccept || useClickedOutsideDismiss;
            if (dismissOptionEnabled == false) return;
            LetterDismissContext.SetTarget(__instance, dismissOption);

            if (enableCloseOnCancel)
            {
                __instance.closeOnCancel = true;
                LetterDismissContext.SetCloseOnCancel(true);
            }
            if (enableCloseOnAccept)
            {
                __instance.closeOnAccept = true;
                LetterDismissContext.SetCloseOnAccept(true);
            }
            if (useClickedOutsideDismiss)
            {
                // Do not force closeOnClickedOutside = true.
                // WindowStack.CloseWindowsBecauseClicked may close this window before
                // Notify_ClickOutsideWindow runs, which would skip our dismiss-option activation.
                LetterDismissContext.SetCloseOnClickedOutside(true);
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

    internal static class Patch_Window_And_WindowStack
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

        private static Window deferredDismissWindowFromClickOutside = null;
        [HarmonyPatch(typeof(Window), nameof(Window.Notify_ClickOutsideWindow))]
        internal static class Patch_Notify_ClickOutsideWindow
        {
            static bool Prefix(Window __instance)
            {
                if (LetterDismissContext.TargetDialog != __instance) return true;
                if (LetterDismissContext.CloseOnClickedOutside == false) return true;

                if (LetterDismissModSettings.Instance.CloseOnClickedOutside == false) return true;

                deferredDismissWindowFromClickOutside = __instance;
                Logger.Message(nameof(Window.Notify_ClickOutsideWindow), $"Dismiss window deferred: '{__instance.GetType().Name}'");

                // Notify_ClickOutsideWindow is called inside WindowStack.NotifyOutsideClicks foreach.
                // Activating dismiss here can modify the windows collection during enumeration
                // and cause "Collection was modified" exceptions.
                // So we defer activation and run it in a Postfix of WindowStack.NotifyOutsideClicks.

                return false; // Skip original method.
            }
        }

        [HarmonyPatch(typeof(WindowStack), nameof(WindowStack_Members.NotifyOutsideClicks))]
        internal static class Patch_WindowStack_NotifyOutsideClicks
        {
#pragma warning disable IDE0051
            static void Postfix()
            {
                if (deferredDismissWindowFromClickOutside != null)
                {
                    Logger.Message(nameof(WindowStack_Members.NotifyOutsideClicks), $"Activate deferred dismiss window: '{deferredDismissWindowFromClickOutside.GetType().Name}'");
                    TryActivateDismissOption(nameof(WindowStack_Members.NotifyOutsideClicks));
                    deferredDismissWindowFromClickOutside = null;
                }
            }
#pragma warning restore IDE0051
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
