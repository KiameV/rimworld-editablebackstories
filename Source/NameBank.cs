using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Verse;

namespace REB_Code
{
	public static class REBPawnNameDatabaseSolid
	{
		private static Dictionary<GenderPossibility, List<NameTriple>> solidNames;
		static REBPawnNameDatabaseSolid()
		{
			REBPawnNameDatabaseSolid.solidNames = new Dictionary<GenderPossibility, List<NameTriple>>();
			foreach (GenderPossibility key in Enum.GetValues(typeof(GenderPossibility)))
			{
				REBPawnNameDatabaseSolid.solidNames.Add(key, new List<NameTriple>());
			}
		}
		public static void AddPlayerContentName(NameTriple newName, GenderPossibility genderPos)
		{
			REBPawnNameDatabaseSolid.solidNames[genderPos].Add(newName);
		}
		public static List<NameTriple> GetListForGender(GenderPossibility gp)
		{
			return REBPawnNameDatabaseSolid.solidNames[gp];
		}
		[DebuggerHidden]
		public static IEnumerable<NameTriple> AllNames()
		{
			foreach (KeyValuePair<GenderPossibility, List<NameTriple>> kvp in REBPawnNameDatabaseSolid.solidNames)
			{
				foreach (NameTriple name in kvp.Value)
				{
					yield return name;
				}
			}
		}
	}

	public static class PKPawnNameDatabaseSolid
	{
		private static Dictionary<GenderPossibility, List<NameTriple>> solidNames;
		static PKPawnNameDatabaseSolid()
		{
			PKPawnNameDatabaseSolid.solidNames = new Dictionary<GenderPossibility, List<NameTriple>>();
			foreach (GenderPossibility key in Enum.GetValues(typeof(GenderPossibility)))
			{
				PKPawnNameDatabaseSolid.solidNames.Add(key, new List<NameTriple>());
			}
		}
		public static void AddPlayerContentName(NameTriple newName, GenderPossibility genderPos)
		{
			PKPawnNameDatabaseSolid.solidNames[genderPos].Add(newName);
		}
		public static List<NameTriple> GetListForGender(GenderPossibility gp)
		{
			return PKPawnNameDatabaseSolid.solidNames[gp];
		}
		[DebuggerHidden]
		public static IEnumerable<NameTriple> AllNames()
		{
			foreach (KeyValuePair<GenderPossibility, List<NameTriple>> kvp in PKPawnNameDatabaseSolid.solidNames)
			{
				foreach (NameTriple name in kvp.Value)
				{
					yield return name;
				}
			}
		}
	}

	public class REB_NameBank
	{
		public PawnNameCategory nameType;
		private List<string>[,] names;
		private readonly static int numGenders = Enum.GetValues(typeof(Gender)).Length;
		private readonly static int numSlots = Enum.GetValues(typeof(REB_NameSlot)).Length;
		string modBasePath = LoadedModManager.RunningMods.First(mcp => mcp.assemblies.loadedAssemblies.Contains(typeof(REB_Initializer).Assembly)).RootDir;
		private IEnumerable<List<string>> AllNameLists
		{
			get
			{
				for (int i = 0; i < REB_NameBank.numGenders; i++)
				{
					for (int j = 0; j < REB_NameBank.numSlots; j++)
					{
						yield return this.names[i, j];
					}
				}
			}
		}
		public REB_NameBank(PawnNameCategory ID)
		{
			this.nameType = ID;
			this.names = new List<string>[REB_NameBank.numGenders, REB_NameBank.numSlots];
			for (int i = 0; i < REB_NameBank.numGenders; i++)
			{
				for (int j = 0; j < REB_NameBank.numSlots; j++)
				{
					this.names[i, j] = new List<string>();
				}
			}
		}
		public void AddNames(REB_NameSlot slot, Gender gender, IEnumerable<string> namesToAdd)
		{
			IEnumerator<string> enumerator = namesToAdd.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					string current = enumerator.Current;
					this.NamesFor(slot, gender).Add(current);
				}
			}
			finally
			{
				if (enumerator == null) { }
				enumerator.Dispose();
			}
		}
		public void ErrorCheck()
		{
			IEnumerator<List<string>> enumerator = this.AllNameLists.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					List<string> current = enumerator.Current;
					List<string> list = (
					from x in current
					group x by x into g
					where g.Count<string>() > 1
					select g.Key).ToList<string>();
					List<string>.Enumerator enumerator1 = list.GetEnumerator();
					try
					{
						while (enumerator1.MoveNext())
						{
							Log.Error(string.Concat("Duplicated name: ", enumerator1.Current));
						}
					}
					finally
					{
						((IDisposable)(object)enumerator1).Dispose();
					}
					List<string>.Enumerator enumerator2 = current.GetEnumerator();
					try
					{
						while (enumerator2.MoveNext())
						{
							string str = enumerator2.Current;
							if (str.Trim() == str)
							{
								continue;
							}
							Log.Error(string.Concat("Trimmable whitespace on name: [", str, "]"));
						}
					}
					finally
					{
						((IDisposable)(object)enumerator2).Dispose();
					}
				}
			}
			finally
			{
				if (enumerator == null) { }
				enumerator.Dispose();
			}
		}
		public string GetName(REB_NameSlot slot, Gender gender = 0, bool checkIfAlreadyUsed = true)
		{
			string str;
			List<string> strs = this.NamesFor(slot, gender);
			int num = 0;
			if (strs.Count == 0)
			{
				Log.Error(string.Concat(new object[] { "Name list for gender=", gender, " slot=", slot, " is empty." }));
				return "Errorname";
			}
			while (true)
			{
				str = strs.RandomElement<string>();
				if (checkIfAlreadyUsed && !NameUseChecker.NameWordIsUsed(str))
				{
					return str;
				}
				num++;
				if (num > 50)
				{
					break;
				}
			}
			return str;
		}
		public List<string> NamesFor(REB_NameSlot slot, Gender gender)
		{
			return this.names[(int)gender, (int)slot];
		}
	}

	public static class REB_FileRead
	{
		public static IEnumerable<string> LinesFromFile(string filePath)
		{
			string rawText = GenFile.TextFromRawFile(filePath);
			foreach (string line in GenText.LinesFromString(rawText))
			{
				yield return line;
			}
		}
	}

	public enum REB_NameSlot : byte
	{
		First,
		Nick,
		Set,
		Last,
		FilterSet1,
		FilterSet2,
		FilterSet3,
		FilterSet4,
		FilterSet5,
		FilterSet6,
		FilterSet7,
		FilterSet8,
		FilterSet9
	}
}
