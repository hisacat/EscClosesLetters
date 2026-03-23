using HisaCat.EscClosesLetters.InternalRefs.Accessors;
using Verse;

namespace HisaCat.EscClosesLetters
{
    [StaticConstructorOnStartup]
    public static class LetterDismissConfig
    {
        // See DiaOption.DefaultOK and ChoiceLetter.Option_Close, ChoiceLetter.Option_Postpone, ChoiceLetter.Option_Reject.
        public static string OkOptionText = string.Empty;
        public static string CloseOptionText = string.Empty;
        public static string PostponeLetterOptionText = string.Empty;
        public static string RejectOptionText = string.Empty;

        public static string[] AllOptionTexts = new string[0];

        public static void UpdateDismissOptionTexts()
        {
            OkOptionText = KeyedKeys.Core.OK.Translate();
            CloseOptionText = KeyedKeys.Core.Close.Translate();
            PostponeLetterOptionText = KeyedKeys.Core.PostponeLetter.Translate();
            RejectOptionText = KeyedKeys.Core.RejectLetter.Translate();

            AllOptionTexts = new[] { OkOptionText, CloseOptionText, PostponeLetterOptionText, RejectOptionText };
            Logger.Message(nameof(UpdateDismissOptionTexts), $"All option texts: {string.Join(", ", AllOptionTexts)}");
        }

        static LetterDismissConfig()
        {
            UpdateDismissOptionTexts();
            Logger.Message($"{nameof(LetterDismissConfig)}.ctor", $"Dismiss option texts updated.");
        }
    }

    public static class LetterDismissContext
    {
        public static bool IsOpeningLetter { get; set; }

        public static Dialog_NodeTree TargetDialog { get; private set; }
        public static DiaOption TargetDismissOption { get; private set; }

        public static bool CloseOnCancel { get; private set; }
        public static bool CloseOnAccept { get; private set; }
        public static bool CloseOnClickedOutside { get; private set; }

        public static void SetTarget(Dialog_NodeTree dialog, DiaOption dismissOption)
            => (TargetDialog, TargetDismissOption) = (dialog, dismissOption);
        public static void ClearTarget()
            => (TargetDialog, TargetDismissOption, CloseOnCancel, CloseOnAccept, CloseOnClickedOutside) = (null, null, false, false, false);
        public static void SetCloseOnCancel(bool value)
            => CloseOnCancel = value;
        public static void SetCloseOnAccept(bool value)
            => CloseOnAccept = value;
        public static void SetCloseOnClickedOutside(bool value)
            => CloseOnClickedOutside = value;
    }

    public static class LetterDismissPolicy
    {
        public static bool CanDismissDialog(Dialog_NodeTree dialog, out DiaOption dismissOption)
        {
            dismissOption = null;

            if (dialog == null) return false;

            var node = dialog.GetCurNode();
            if (node == null) return false;

            var options = node.options;
            if (options == null) return false;

            var settings = LetterDismissModSettings.Instance;

            foreach (var option in options)
            {
                var text = option.GetText();
                if (string.IsNullOrEmpty(text)) continue;

                Logger.Message(nameof(CanDismissDialog), $"Checking option '{text}'");

                if (text.Equals(LetterDismissConfig.OkOptionText) && settings.UseOkOptionForDismiss)
                {
                    Logger.Message(nameof(CanDismissDialog), $"{nameof(LetterDismissConfig.OkOptionText)} '{LetterDismissConfig.OkOptionText}' detected.");
                    dismissOption = option;
                    return true;
                }
                if (text.Equals(LetterDismissConfig.CloseOptionText) && settings.UseCloseOptionForDismiss)
                {
                    Logger.Message(nameof(CanDismissDialog), $"{nameof(LetterDismissConfig.CloseOptionText)} '{LetterDismissConfig.CloseOptionText}' detected.");
                    dismissOption = option;
                    return true;
                }
                if (text.Equals(LetterDismissConfig.PostponeLetterOptionText) && settings.UsePostponeLetterOptionForDismiss)
                {
                    Logger.Message(nameof(CanDismissDialog), $"{nameof(LetterDismissConfig.PostponeLetterOptionText)} '{LetterDismissConfig.PostponeLetterOptionText}' detected.");
                    dismissOption = option;
                    return true;
                }
                if (text.Equals(LetterDismissConfig.RejectOptionText) && settings.UseRejectOptionForDismiss)
                {
                    Logger.Message(nameof(CanDismissDialog), $"{nameof(LetterDismissConfig.RejectOptionText)} '{LetterDismissConfig.RejectOptionText}' detected.");
                    dismissOption = option;
                    return true;
                }
            }
            Logger.Message(nameof(CanDismissDialog), "No dismiss option detected.");

            return false;
        }
    }

    public static class Logger
    {
        public static void Message(string message, bool force = false)
        {
            if ((force || LetterDismissModSettings.Instance.EnableLog) == false) return;
            Log.Message($"[{nameof(EscClosesLetters)}] {message}");
        }
        public static void Message(string methodName, string message, bool force = false) => Message($"{methodName}: {message}", force);
    }
}
