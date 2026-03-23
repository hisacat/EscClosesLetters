using UnityEngine;
using Verse;

namespace HisaCat.EscClosesLetters
{
    public class LetterDismissModSettings : ModSettings
    {
        public ConfigValue<bool> useCancelKeyToDismiss = new(true);
        public bool UseCancelKeyToDismiss => useCancelKeyToDismiss.value;
        public ConfigValue<bool> useAcceptKeyToDismiss = new(true);
        public bool UseAcceptKeyToDismiss => useAcceptKeyToDismiss.value;
        public ConfigValue<bool> closeOnClickedOutside = new(true);
        public bool CloseOnClickedOutside => closeOnClickedOutside.value;

        public ConfigValue<bool> useOkOptionForDismiss = new(true);
        public bool UseOkOptionForDismiss => useOkOptionForDismiss.value;
        public ConfigValue<bool> useCloseOptionForDismiss = new(true);
        public bool UseCloseOptionForDismiss => useCloseOptionForDismiss.value;
        public ConfigValue<bool> usePostponeLetterOptionForDismiss = new(false);
        public bool UsePostponeLetterOptionForDismiss => usePostponeLetterOptionForDismiss.value;
        public ConfigValue<bool> useRejectOptionForDismiss = new(false);
        public bool UseRejectOptionForDismiss => useRejectOptionForDismiss.value;
        public ConfigValue<bool> enableLog = new(false);
        public bool EnableLog => enableLog.value;

        public class ConfigValue<T>
        {
            public T value;
            public readonly T defaultValue;
            public ConfigValue(T defaultValue)
            {
                this.value = defaultValue;
                this.defaultValue = defaultValue;
            }

            public void ResetToDefault() => this.value = this.defaultValue;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.useCancelKeyToDismiss.value, nameof(this.useCancelKeyToDismiss), this.useCancelKeyToDismiss.defaultValue);
            Scribe_Values.Look(ref this.useAcceptKeyToDismiss.value, nameof(this.useAcceptKeyToDismiss), this.useAcceptKeyToDismiss.defaultValue);
            Scribe_Values.Look(ref this.closeOnClickedOutside.value, nameof(this.closeOnClickedOutside), this.closeOnClickedOutside.defaultValue);

            Scribe_Values.Look(ref this.useOkOptionForDismiss.value, nameof(this.useOkOptionForDismiss), this.useOkOptionForDismiss.defaultValue);
            Scribe_Values.Look(ref this.useCloseOptionForDismiss.value, nameof(this.useCloseOptionForDismiss), this.useCloseOptionForDismiss.defaultValue);
            Scribe_Values.Look(ref this.usePostponeLetterOptionForDismiss.value, nameof(this.usePostponeLetterOptionForDismiss), this.usePostponeLetterOptionForDismiss.defaultValue);
            Scribe_Values.Look(ref this.useRejectOptionForDismiss.value, nameof(this.useRejectOptionForDismiss), this.useRejectOptionForDismiss.defaultValue);

            Scribe_Values.Look(ref this.enableLog.value, nameof(this.enableLog), this.enableLog.defaultValue);
            base.ExposeData();
        }

        public void ResetToDefaults()
        {
            this.useCancelKeyToDismiss.ResetToDefault();
            this.useAcceptKeyToDismiss.ResetToDefault();
            this.closeOnClickedOutside.ResetToDefault();

            this.useOkOptionForDismiss.ResetToDefault();
            this.useCloseOptionForDismiss.ResetToDefault();
            this.usePostponeLetterOptionForDismiss.ResetToDefault();
            this.useRejectOptionForDismiss.ResetToDefault();

            this.enableLog.ResetToDefault();
        }

        private static LetterDismissModSettings _instance = null;
        public static LetterDismissModSettings Instance
        {
            get => _instance ??= LoadedModManager.GetMod<LetterDismissMod>().GetSettings<LetterDismissModSettings>();
        }
    }

    public class LetterDismissMod : Mod
    {
        private readonly LetterDismissModSettings settings = null;
        public LetterDismissMod(ModContentPack content) : base(content)
        {
            this.settings = this.GetSettings<LetterDismissModSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new();
            listingStandard.Begin(inRect);
            {
                listingStandard.Label(KeyedKeys.LetterDismiss.Settings.UseKeysToDismiss.Translate());
                listingStandard.CheckboxLabeled(KeyedKeys.LetterDismiss.Settings.UseCancelKeyToDismiss.Translate(RimWorld.KeyBindingDefOf.Cancel.MainKeyLabel), ref this.settings.useCancelKeyToDismiss.value);
                listingStandard.CheckboxLabeled(KeyedKeys.LetterDismiss.Settings.UseAcceptKeyToDismiss.Translate(RimWorld.KeyBindingDefOf.Accept.MainKeyLabel), ref this.settings.useAcceptKeyToDismiss.value);
                listingStandard.CheckboxLabeled(KeyedKeys.LetterDismiss.Settings.CloseOnClickedOutside.Translate(), ref this.settings.closeOnClickedOutside.value);
                listingStandard.Gap();

                listingStandard.Label(KeyedKeys.LetterDismiss.Settings.UseOptionsToDismiss.Translate());
                listingStandard.CheckboxLabeled(KeyedKeys.LetterDismiss.Settings.UseOkOptionForDismiss.Translate("OK".Translate()), ref this.settings.useOkOptionForDismiss.value);
                listingStandard.CheckboxLabeled(KeyedKeys.LetterDismiss.Settings.UseCloseOptionForDismiss.Translate("Close".Translate()), ref this.settings.useCloseOptionForDismiss.value);
                listingStandard.CheckboxLabeled(KeyedKeys.LetterDismiss.Settings.UsePostponeLetterOptionForDismiss.Translate("PostponeLetter".Translate()), ref this.settings.usePostponeLetterOptionForDismiss.value);
                listingStandard.CheckboxLabeled(KeyedKeys.LetterDismiss.Settings.UseRejectOptionForDismiss.Translate("RejectLetter".Translate()), ref this.settings.useRejectOptionForDismiss.value);
                listingStandard.Gap();

                listingStandard.Label(KeyedKeys.LetterDismiss.Settings.Debug.Translate());
                listingStandard.CheckboxLabeled(KeyedKeys.LetterDismiss.Settings.EnableLog.Translate(), ref this.settings.enableLog.value);
                listingStandard.Gap();

                if (listingStandard.ButtonText(KeyedKeys.LetterDismiss.Settings.ResetToDefaults.Translate()))
                {
                    this.settings.ResetToDefaults();
                }
                listingStandard.Gap();
            }
            listingStandard.End();

            // if (dirty) this.WriteSettings();

            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
            => KeyedKeys.LetterDismiss.EscClosesLetters.Translate();
    }
}
