using System;

namespace LegendaryService.Models
{
	/*public class GameData
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public GamePackageType PackageType { get; set; }
		public string CoverImage { get; set; }
		public GameBaseMap BaseMap { get; set; }
		public Ally[] Allies { get; set; }
		public Henchman[] Henchmen { get; set; }
		public Adversary[] Adversaries { get; set; }
		public Mastermind[] Masterminds { get; set; }
		public Scheme[] Schemes { get; set; }
		public Neutral[] Neutral { get; set; }
	}

	/*public class Ally
	{
		public string Name { get; set; }
		public string Team { get; set; }
		public string Requires { get; set; }
		public string[] Classes { get; set; }
		public string[] Abilities { get; set; }
	}* /

	public class Henchman
	{
		public string Name { get; set; }
		public string[] Abilities { get; set; }
	}

	public class Adversary
	{
		public string Name { get; set; }
		public string[] Abilities { get; set; }
	}

	public class Mastermind
	{
		public string Name { get; set; }
		public string RequiresAllOf { get; set; }
		public bool HasEpicSide { get; set; }
		public Henchman[] RequiresOneOfHenchmen { get; set; }
		public Adversary[] RequiresOneOfAdversaries { get; set; }
		public Ally[] RequiresOneOfAllies { get; set; }
		public Neutral[] RequiresOneOfNeutrals { get; set; }
		public string[] Abilities { get; set; }
	}

	public class Scheme
	{
		public string Name { get; set; }
		public string SchemeTwistCount { get; set; }
		public string ExtraHenchmenGroups { get; set; }
		public string Requires { get; set; }
		public string ExtraHeroGroups { get; set; }
		public string ExtraVillainGroups { get; set; }
		public string RequiresAllOf { get; set; }
		public string ExtraBystanders { get; set; }
		public AllyTeam RequiredTeams { get; set; }
		public int ExtraMastermindGroups { get; set; }
		public string ExtraMasterminds { get; set; }
		public string RequiresSomeOf { get; set; }
		public string[] Abilities { get; set; }
	}

	public class Neutral
	{
		public string name { get; set; }
	}*/


}