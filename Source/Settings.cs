using UnityEngine;
using Verse;


namespace REB_Code
{
    public class Controller : Mod
    {
        public static Settings Settings;
        public override string SettingsCategory() { return "REB.EditableBackstories".Translate(); }
        public override void DoSettingsWindowContents(Rect canvas) { Settings.DoWindowContents(canvas); }
        public Controller(ModContentPack content) : base(content)
        {
            Settings = GetSettings<Settings>();
        }
    }

    public class Settings : ModSettings
    {
        public bool useLiteMode = false;
        public void DoWindowContents(Rect canvas)
        {
            Listing_Standard list = new Listing_Standard();
            list.ColumnWidth = canvas.width;
            list.Begin(canvas);
            list.Gap();
            list.CheckboxLabeled("REB.UseLiteMode".Translate(), ref useLiteMode, "REB.UseLiteModeTip".Translate());
            list.End();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref useLiteMode, "useLiteMode", false);
        }
    }
}
