using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace REB_Code
{
	public class Controller : Mod
	{
		public static bool settingsChanged = false;
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
		public bool categorizeSource = false;
		public bool allowVanillaTriples = false;
		public void DoWindowContents(Rect canvas)
		{
			Listing_Standard list = new Listing_Standard
			{
				ColumnWidth = canvas.width
			};
			list.Begin(canvas);
			list.Gap();
			list.CheckboxLabeled("REB.UseLiteMode".Translate(), ref useLiteMode, "REB.UseLiteModeTip".Translate());
			list.Gap();
			list.CheckboxLabeled("REB.CategorizeSource".Translate(), ref categorizeSource, "REB.CategorizeSourceTip".Translate());
			list.Gap();
			list.CheckboxLabeled("REB.AllowVanillaTriples".Translate(), ref allowVanillaTriples, "REB.AllowVanillaTriplesTip".Translate());
			list.End();
		}
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref useLiteMode, "useLiteMode", false);
			Scribe_Values.Look(ref categorizeSource, "categorizeSource", false);
			Scribe_Values.Look(ref allowVanillaTriples, "allowVanillaTriples", false);
			Controller.settingsChanged = true;
		}
	}
}
