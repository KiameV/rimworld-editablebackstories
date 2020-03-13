using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace REB_Code
{
	public class BackstoryDef : Def
	{
		public string baseDesc;
		public string bodyTypeGlobal;
		public string bodyTypeMale;
		public string bodyTypeFemale;
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

		private static readonly FieldInfo BodyTypeGlobalFI = typeof(Backstory).GetField("bodyTypeGlobal", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo BodyTypeGlobalResolvedFI = typeof(Backstory).GetField("bodyTypeGlobalResolved", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo BodyTypeMaleFI = typeof(Backstory).GetField("bodyTypeMale", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo BodyTypeMaleResolvedFI = typeof(Backstory).GetField("bodyTypeMaleResolved", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo BodyTypeFemaleFI = typeof(Backstory).GetField("bodyTypeFemale", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo BodyTypeFemaleResolvedFI = typeof(Backstory).GetField("bodyTypeFemaleResolved", BindingFlags.Instance | BindingFlags.NonPublic);

		public static BackstoryDef Named(string defName)
		{
			return DefDatabase<BackstoryDef>.GetNamed(defName);
		}

		public override void ResolveReferences()
		{
			base.ResolveReferences();
			if (!this.addToDatabase) return;
			if (BackstoryDatabase.allBackstories.ContainsKey(this.UniqueSaveKey()))
			{
				Log.Error("Backstory Error (" + this.defName + "): Duplicate defName.");
				return;
			}
			Backstory b = new Backstory();
			if (!string.IsNullOrEmpty(this.title) || !string.IsNullOrEmpty(this.titleFemale))
				b.SetTitle((string.IsNullOrEmpty(this.title) ? this.titleFemale : this.title), (string.IsNullOrEmpty(this.titleFemale) ? this.title : this.titleFemale));
			else
			{
				return;
			}
			if (!string.IsNullOrEmpty(titleShort) || !string.IsNullOrEmpty(titleShortFemale))
				b.SetTitleShort((string.IsNullOrEmpty(this.titleShort) ? this.titleShortFemale : this.titleShort), (string.IsNullOrEmpty(this.titleShortFemale) ? this.titleShort : this.titleShortFemale));
			else
				b.SetTitleShort((string.IsNullOrEmpty(this.title) ? this.titleFemale : this.title), (string.IsNullOrEmpty(this.titleFemale) ? this.title : this.titleFemale));
			if (!baseDesc.NullOrEmpty())
				b.baseDesc = baseDesc;
			else
			{
				b.baseDesc = "Empty.";
			}
			bool bodyTypeSet = false;
			if (!string.IsNullOrEmpty(bodyTypeGlobal))
			{
				bodyTypeSet = SetGlobalBodyType(b, bodyTypeGlobal);
			}
			if (!bodyTypeSet)
			{
				if (!SetMaleBodyType(b, bodyTypeMale))
				{
					SetMaleBodyType(b, "Male");
				}
				if (!SetFemaleBodyType(b, bodyTypeFemale))
				{
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
				for (int i = 0; i < spawnCategories.Count; i++)
				{
					if (Util.Categories.Contains(spawnCategories[i]))
					{
						spawnCategories[i] = "REB" + spawnCategories[i];
					}
				}
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
			bool skip = false;
			foreach (var s in b.ConfigErrors(false))
			{
				Log.Error("Backstory Error (" + b.identifier + "): " + s);
				skip = true;
			}
			if (!skip)
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
				BackstoryDatabase.allBackstories[b.identifier] = b;
			}
		}

		private static bool SetGlobalBodyType(Backstory b, string s)
		{
			if (TryGetBodyTypeDef(s, out BodyTypeDef def))
			{
				BodyTypeGlobalFI.SetValue(b, def.ToString());
				BodyTypeGlobalResolvedFI.SetValue(b, def);
				return true;
			}
			return false;
		}

		private static bool SetMaleBodyType(Backstory b, string s)
		{
			if (TryGetBodyTypeDef(s, out BodyTypeDef def))
			{
				BodyTypeMaleFI.SetValue(b, def.ToString());
				BodyTypeMaleResolvedFI.SetValue(b, def);
				return true;
			}
			return false;
		}

		private static bool SetFemaleBodyType(Backstory b, string s)
		{
			if (TryGetBodyTypeDef(s, out BodyTypeDef def))
			{
				BodyTypeFemaleFI.SetValue(b, def.ToString());
				BodyTypeFemaleResolvedFI.SetValue(b, def);
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

	public static class BackstoryDefExt
	{
		public static string UniqueSaveKey(this BackstoryDef def)
		{
			return "REB_" + def.defName;
		}
	}

	public struct BackstoryDefSkillListItem
	{
		public string key;
		public int value;
	}

	public struct BackstoryDefTraitListItem
	{
		public string key;
		public int value;
	}

	//
	//	Read Names From XML Files
	//

	public class NameDef : Def
	{
		#region XML Data
		public GenderPossibility gender;
		public NameTriple name;
		public string childhoodStory;
		public string adulthoodStory;
		#endregion
		public static NameDef Named(string defName)
		{
			return DefDatabase<NameDef>.GetNamed(defName);
		}
		public PawnBioType BioType
		{
			get { return PawnBioType.Undefined; }
		}
		public override void ResolveReferences()
		{
			base.ResolveReferences();
			PawnBio bio = new PawnBio
			{
				name = name,
				gender = gender
			};
			if (bio.gender != GenderPossibility.Male && bio.gender != GenderPossibility.Female)
			{
				bio.gender = GenderPossibility.Either;
			}
			bio.PostLoad();
			if (bio.name.First.NullOrEmpty() || bio.name.Last.NullOrEmpty())
			{
				if (!childhoodStory.NullOrEmpty() || !adulthoodStory.NullOrEmpty())
				{
					Log.Error("Backstory Error (" + bio.name + "): A locked backstory can only be attached to a full name.");
				}
				if (bio.name.First.NullOrEmpty() && bio.name.Last.NullOrEmpty())
				{
					if (!bio.name.Nick.NullOrEmpty())
					{
						if (bio.gender == GenderPossibility.Male)
						{
							REB_Initializer.NamesNicksMale.Add(bio.name.Nick);
						}
						else if (bio.gender == GenderPossibility.Female)
						{
							REB_Initializer.NamesNicksFemale.Add(bio.name.Nick);
						}
						else
						{
							REB_Initializer.NamesNicksUnisex.Add(bio.name.Nick);
						}
					}
				}
				else if (bio.name.First.NullOrEmpty())
				{
					if (!bio.name.Last.NullOrEmpty())
					{
						REB_Initializer.NamesLast.Add(bio.name.Last);
					}
				}
				else if (bio.name.Last.NullOrEmpty())
				{
					if (!bio.name.First.NullOrEmpty())
					{
						if (bio.gender == GenderPossibility.Male)
						{
							REB_Initializer.NamesFirstMale.Add(bio.name.First);
						}
						else if (bio.gender == GenderPossibility.Female)
						{
							REB_Initializer.NamesFirstFemale.Add(bio.name.First);
						}
						else
						{
							REB_Initializer.NamesFirstMale.Add(bio.name.First);
							REB_Initializer.NamesFirstFemale.Add(bio.name.First);
						}
					}
				}
			}
			else
			{
				bio.name.ResolveMissingPieces(null);
				if ((!childhoodStory.NullOrEmpty() && adulthoodStory.NullOrEmpty())
				  || (childhoodStory.NullOrEmpty() && !adulthoodStory.NullOrEmpty()))
				{
					Log.Error("Backstory Error (" + bio.name + "): A locked backstory must include both a childhood story and an adulthood story.");
				}
				if (!childhoodStory.NullOrEmpty() && !BackstoryDatabase.allBackstories.ContainsKey("REB_" + childhoodStory))
				{
					Log.Error("Backstory Error (" + bio.name + "): Childhood backstory '" + childhoodStory + "' does not exist.");
					childhoodStory = "";
				}
				if (!adulthoodStory.NullOrEmpty() && !BackstoryDatabase.allBackstories.ContainsKey("REB_" + adulthoodStory))
				{
					Log.Error("Backstory Error (" + bio.name + "): Adulthood backstory '" + adulthoodStory + "' does not exist.");
					adulthoodStory = "";
				}
				if (!childhoodStory.NullOrEmpty() && !adulthoodStory.NullOrEmpty())
				{
					bio.childhood = BackstoryDatabase.allBackstories["REB_" + childhoodStory];
					bio.adulthood = BackstoryDatabase.allBackstories["REB_" + adulthoodStory];
					bio.pirateKing = true;
					SolidBioDatabase.allBios.Add(bio);
					REB_Initializer.fullBioCount++;
				}
				else
				{
					REBPawnNameDatabaseSolid.AddPlayerContentName(bio.name, bio.gender);
					REB_Initializer.fullCount++;
				}
			}
		}
	}
}