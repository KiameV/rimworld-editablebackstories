using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace REB_Code
{
    class BackstoryDef : Def
    {
        #region XML Data
        public string baseDesc;
        public string bodyTypeGlobal = "Undefined";
        public string bodyTypeMale = "Undefined";
        public string bodyTypeFemale = "Undefined";
        public string title;
        public string titleFemale;
        public string titleShort;
        public string titleShortFemale;
        public BackstorySlot slot = BackstorySlot.Adulthood;
        public bool shuffleable = true;
        public bool addToDatabase = true;
        public List<WorkTags> workAllows = new List<WorkTags>();
        public List<WorkTags> workDisables = new List<WorkTags>();
        public List<WorkTags> requiredWorkTags = new List<WorkTags>();
        public List<BackstoryDefSkillListItem> skillGains = new List<BackstoryDefSkillListItem>();
        public List<string> spawnCategories = new List<string>();
        public List<BackstoryDefTraitListItem> forcedTraits = new List<BackstoryDefTraitListItem>();
        public List<BackstoryDefTraitListItem> disallowedTraits = new List<BackstoryDefTraitListItem>();
        #endregion

        public static BackstoryDef Named(string defName)
        {
            return DefDatabase<BackstoryDef>.GetNamed(defName);
        }

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            if (!this.addToDatabase) return;
            if (Controller.Settings.useLiteMode.Equals(true))
            {
                if (BackstoryDatabase.allBackstories.ContainsKey(this.UniqueSaveKey()))
                {
                    Log.Error("Backstory Error (" + this.defName + "): Duplicate defName.");
                    return;
                }
            }
            else
            {
                if (REB_Initializer.REB_Backstories.ContainsKey(this.UniqueSaveKey()))
                {
                    Log.Error("Backstory Error (" + this.defName + "): Duplicate defName.");
                    return;
                }
            }
            Backstory b = new Backstory();
            if (!string.IsNullOrEmpty(this.title) ||
                !string.IsNullOrEmpty(this.titleFemale))
                b.SetTitle(
                    (string.IsNullOrEmpty(this.title) ? this.titleFemale : this.title),
                    (string.IsNullOrEmpty(this.titleFemale) ? this.title : this.titleFemale));
            else
            {
                return;
            }
            if (!string.IsNullOrEmpty(titleShort) ||
                !string.IsNullOrEmpty(titleShortFemale))
                b.SetTitleShort(
                    (string.IsNullOrEmpty(this.titleShort) ? this.titleShortFemale : this.titleShort),
                    (string.IsNullOrEmpty(this.titleShortFemale) ? this.titleShort : this.titleShortFemale));
            else
                b.SetTitleShort(
                    (string.IsNullOrEmpty(this.title) ? this.titleFemale : this.title),
                    (string.IsNullOrEmpty(this.titleFemale) ? this.title : this.titleFemale));
            if (!string.IsNullOrEmpty(baseDesc))
                b.baseDesc = baseDesc;
            else
            {
                b.baseDesc = "Empty.";
            }

            bool bodyTypeSet = false;
            if (!string.IsNullOrEmpty(bodyTypeGlobal))
            {
                bodyTypeSet = SetGlobalBodyType(b, bodyTypeGlobal);
                if (!bodyTypeSet)
                {
                    //.Warning(b.title + " could not load Global Body Type of [" + bodyTypeGlobal + "]. Will try Male/Female.");
                }
            }

            if (!bodyTypeSet)
            {
                if (!SetMaleBodyType(b, bodyTypeMale) ||
                    !SetFemaleBodyType(b, bodyTypeFemale))
                {
                    //Log.Error(b.title + " unable to load Female Body Type of [" + bodyTypeFemale + "] or Male Body Type of [" + bodyTypeMale + "]. Backstory cannot be loaded.");

                    SetMaleBodyType(b, "Male");
                    SetFemaleBodyType(b, "Female");
                }
            }

            b.slot = slot;
            b.shuffleable = shuffleable;
            if (spawnCategories.NullOrEmpty())
            {
                return;
            }
            else
                b.spawnCategories = spawnCategories;
            if (workAllows.Count > 0)
            {
                foreach (WorkTags current in Enum.GetValues(typeof(WorkTags)))
                {
                    if (!workAllows.Contains(current))
                    {
                        b.workDisables |= current;
                    }
                }
            }
            else if (workDisables.Count > 0)
            {
                foreach (var tag in workDisables)
                {
                    b.workDisables |= tag;
                }
            }
            else
            {
                b.workDisables = WorkTags.None;
            }
            if (requiredWorkTags.Count > 0)
            {
                foreach (var tag in requiredWorkTags)
                {
                    b.requiredWorkTags |= tag;
                }
            }
            else
            {
                b.requiredWorkTags = WorkTags.None;
            }

            Dictionary<SkillDef, int> d = new Dictionary<SkillDef, int>();
            foreach (BackstoryDefSkillListItem i in skillGains)
            {
                SkillDef def = DefDatabase<SkillDef>.GetNamed(i.key, false);
                if (def == null)
                {
                    if (i.key.Equals("Growing"))
                    {
                        Log.Message("Editable Backstories: Converting SkillDef of [" + i.key + "] to [" + SkillDefOf.Plants.ToString() + "] for Backstory.Title [" + b.title + "]");
                        def = SkillDefOf.Plants;
                    }
                    else
                    {
                        Log.Error("Editable Backstories: Unable to find SkillDef of [" + i.key + "] for Backstory.Title [" + b.title + "]");
                    }
                }

                if (def != null)
                {
                    d.Add(def, i.value);
                }
            }

            b.skillGainsResolved = d;
            Dictionary<string, int> fTraitList = forcedTraits.ToDictionary(i => i.key, i => i.value);
            if (fTraitList.Count > 0)
            {
                b.forcedTraits = new List<TraitEntry>();
                foreach (KeyValuePair<string, int> trait in fTraitList)
                {
                    b.forcedTraits.Add(new TraitEntry(TraitDef.Named(trait.Key), trait.Value));
                }
            }
            Dictionary<string, int> dTraitList = disallowedTraits.ToDictionary(i => i.key, i => i.value);
            if (dTraitList.Count > 0)
            {
                b.disallowedTraits = new List<TraitEntry>();
                foreach (KeyValuePair<string, int> trait in dTraitList)
                {
                    b.disallowedTraits.Add(new TraitEntry(TraitDef.Named(trait.Key), trait.Value));
                }
            }
            b.ResolveReferences();
            b.PostLoad();
            b.identifier = this.UniqueSaveKey();

            bool flag = false;
            foreach (var s in b.ConfigErrors(false))
            {
                Log.Error("Backstory Error (" + b.identifier + "): " + s);
                if (!flag)
                {
                    flag = true;
                }
            }
            if (!flag)
            {
                if (b.slot.Equals(BackstorySlot.Adulthood))
                {
                    if (b.shuffleable.Equals(true))
                    {
                        REB_Initializer.adultCount++;
                    }
                    else
                    {
                        REB_Initializer.adultNSCount++;
                    }
                }
                if (b.slot.Equals(BackstorySlot.Childhood))
                {
                    if (b.shuffleable.Equals(true))
                    {
                        REB_Initializer.childCount++;
                    }
                    else
                    {
                        REB_Initializer.childNSCount++;
                    }
                }
                if (Controller.Settings.useLiteMode.Equals(true))
                {
                    BackstoryDatabase.allBackstories.Add(b.identifier, b);
                }
                else
                {
                    REB_Initializer.REB_Backstories.Add(b.identifier, b);
                }
            }
        }

        private static bool SetGlobalBodyType(Backstory b, string s)
        {
            BodyTypeDef def;
            if (TryGetBodyTypeDef(s, out def))
            {
                typeof(Backstory).GetField("bodyTypeGlobal", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(b, def.ToString());
                typeof(Backstory).GetField("bodyTypeGlobalResolved", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(b, def);
                return true;
            }
            return false;
        }

        private static bool SetMaleBodyType(Backstory b, string s)
        {
            BodyTypeDef def;
            if (TryGetBodyTypeDef(s, out def))
            {
                typeof(Backstory).GetField("bodyTypeMale", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(b, def.ToString());
                typeof(Backstory).GetField("bodyTypeMaleResolved", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(b, def);
                return true;
            }
            return false;
        }

        private static bool SetFemaleBodyType(Backstory b, string s)
        {
            BodyTypeDef def;
            if (TryGetBodyTypeDef(s, out def))
            {
                typeof(Backstory).GetField("bodyTypeFemale", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(b, def.ToString());
                typeof(Backstory).GetField("bodyTypeFemaleResolved", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(b, def);
                return true;
            }
            return false;
        }

        private static bool TryGetBodyTypeDef(string s, out BodyTypeDef def)
        {
            if (string.IsNullOrEmpty(s))
            {
                def = null;
                return false;
            }

            def = DefDatabase<BodyTypeDef>.GetNamed(s, false);

            if (def == null)
                return false;
            return true;
        }
    }

    static class BackstoryDefExt
    {
        public static string UniqueSaveKey(this BackstoryDef def)
        {
            return "REB_" + def.defName;
        }
    }

    struct BackstoryDefSkillListItem
    {
        public string key;
        public int value;
    }

    struct BackstoryDefTraitListItem
    {
        public string key;
        public int value;
    }
}
