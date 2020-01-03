using LegendaryService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LegendaryService.GameService;

namespace LegendaryClientConsole.Utility
{
	public static class CardRequirementUtility
	{
		public static async ValueTask<IEnumerable<CardRequirement>> GetCardRequirements(GameServiceClient client, int currentGamePackageId, bool? defaultOption)
		{
			List<CardRequirement> cardRequirements = new List<CardRequirement>();

			if (ConsoleUtility.GetUserInputBool("Add card requirements:", defaultOption))
				cardRequirements.AddRange(await GetSingleCardRequirement(client, currentGamePackageId));

			while (cardRequirements.Count != 0 && ConsoleUtility.GetUserInputBool("Add another requirement?", false))
				cardRequirements.AddRange(await GetSingleCardRequirement(client, currentGamePackageId));

			return cardRequirements;
		}

		public static async ValueTask<IEnumerable<CardRequirement>> GetSingleCardRequirement(GameServiceClient client, int currentGamePackageId)
		{
			var result = ConsoleUtility.GetUserInputRequiredValue("What type of requirement is this?", "Count", "AdditionalCards");

			if (result == "Count")
				return GetCardCountRequirements();
			if (result == "AdditionalCards")
				return await GetAdditionalCardRequirements(client, currentGamePackageId);

			return new CardRequirement[] { };
		}

		private static IEnumerable<CardRequirement> GetCardCountRequirements()
		{
			var cardType = ConsoleUtility.GetUserInputRequiredValue("What type of card count is changing?", "Ally", "Adversary", "Mastermind", "Henchman", "Bystander");
			var changesBasedOnPlayerCount = ConsoleUtility.GetUserInputBool("Does the card count change based on number of players?");

			if (!changesBasedOnPlayerCount)
			{
				int additionalSets = ConsoleUtility.GetUserInputInt("Additional sets: ");

				yield return cardType switch
				{
					"Ally" => new CardRequirement { AdditionalSetCount = additionalSets, CardSetType = CardSetType.CardSetAlly },
					"Adversary" => new CardRequirement { AdditionalSetCount = additionalSets, CardSetType = CardSetType.CardSetAdversary },
					"Mastermind" => new CardRequirement { AdditionalSetCount = additionalSets, CardSetType = CardSetType.CardSetMastermind },
					"Henchman" => new CardRequirement { AdditionalSetCount = additionalSets, CardSetType = CardSetType.CardSetHenchman },
					"Bystander" => new CardRequirement { AdditionalSetCount = additionalSets, CardSetType = CardSetType.CardSetBystander },
					_ => throw new Exception("Failed to match on cardType")
				};
			}
			else
			{
				for (int i = 1; i <= 5; i++)
				{
					var playerMessage = i == 1 ? "player" : "players";
					int additionalSets = ConsoleUtility.GetUserInputInt($"Additional sets for {i} {playerMessage}: ");

					if (additionalSets != 0)
					{
						yield return cardType switch
						{
							"Ally" => new CardRequirement { AdditionalSetCount = additionalSets, CardSetType = CardSetType.CardSetAlly, PlayerCount = i },
							"Adversary" => new CardRequirement { AdditionalSetCount = additionalSets, CardSetType = CardSetType.CardSetAdversary, PlayerCount = i },
							"Mastermind" => new CardRequirement { AdditionalSetCount = additionalSets, CardSetType = CardSetType.CardSetMastermind, PlayerCount = i },
							"Henchman" => new CardRequirement { AdditionalSetCount = additionalSets, CardSetType = CardSetType.CardSetHenchman, PlayerCount = i },
							"Bystander" => new CardRequirement { AdditionalSetCount = additionalSets, CardSetType = CardSetType.CardSetBystander, PlayerCount = i },
							_ => throw new Exception("Failed to match on cardType")
						};
					}
				}
			}
		}

		private static async ValueTask<IEnumerable<CardRequirement>> GetAdditionalCardRequirements(GameServiceClient client, int currentGamePackageId)
		{
			var cardType = ConsoleUtility.GetUserInputRequiredValue("What type of item is required?", "Ally", "Adversary", "Mastermind", "Henchman", "Neutral");
			
			var cardRequirement = new CardRequirement();
			
			cardRequirement = cardType switch
			{
				"Ally" => await AddAllyCardIds(cardRequirement, currentGamePackageId, client),
				"Adversary" => await AddAdversaryCardIds(cardRequirement, currentGamePackageId, client),
				"Mastermind" => await AddMastermindCardIds(cardRequirement, currentGamePackageId, client),
				"Henchman" => await AddHenchmanCardIds(cardRequirement, currentGamePackageId, client),
				"Neutral" => await AddNeutralCardIds(cardRequirement, currentGamePackageId, client),
				_ => throw new Exception("Failed to match on cardType")
			};

			return new[] { cardRequirement };
		}

		private static async ValueTask<CardRequirement> AddAllyCardIds(CardRequirement requirement, int currentGamePackageId, GameServiceClient client)
		{
			Ally allyInGamePackage = null;

			while (allyInGamePackage == null)
			{
				var itemName = ConsoleUtility.GetUserInput("Ally Name: ");

				var allies = await AllyUtility.GetAlliesAsync(client, name: itemName, nameMatchStyle: NameMatchStyle.Similar);

				if (allies.Count == 1)
					allyInGamePackage = allies.First();

				if (allies.Count == 0)
					ConsoleUtility.WriteLine($"Ally '{itemName}' was not found.");

				if (allies.Count > 1)
				{
					allyInGamePackage = allies.FirstOrDefault(x => x.GamePackageId == currentGamePackageId);
					if (allyInGamePackage == null)
						ConsoleUtility.WriteLine($"Too many Allies match '{itemName}' and none were in the current GamePackage.");
				}
			}

			requirement.RequiredSetId = allyInGamePackage.Id;
			requirement.CardSetType = CardSetType.CardSetAlly;
			ConsoleUtility.WriteLine($"Adding specific requirement for Ally: {allyInGamePackage}");

			return requirement;
		}

		private static async ValueTask<CardRequirement> AddAdversaryCardIds(CardRequirement requirement, int currentGamePackageId, GameServiceClient client)
		{
			Adversary adversaryInGamePackage = null;

			while (adversaryInGamePackage == null)
			{
				var itemName = ConsoleUtility.GetUserInput("Adversary Name: ");

				var adversaries = await AdversaryUtility.GetAdversariesAsync(client, name: itemName, nameMatchStyle: NameMatchStyle.Similar);

				if (adversaries.Count == 1)
					adversaryInGamePackage = adversaries.First();

				if (adversaries.Count == 0)
					ConsoleUtility.WriteLine($"Adversary '{itemName}' was not found.");

				if (adversaries.Count > 1)
				{
					adversaryInGamePackage = adversaries.FirstOrDefault(x => x.GamePackageId == currentGamePackageId);
					if (adversaryInGamePackage == null)
						ConsoleUtility.WriteLine($"Too many Adversaries match '{itemName}' and none were in the current GamePackage.");
				}
			}

			requirement.RequiredSetId = adversaryInGamePackage.Id;
			requirement.CardSetType = CardSetType.CardSetAdversary;
			ConsoleUtility.WriteLine($"Adding specific requirement for Adversary: {adversaryInGamePackage}");

			return requirement;
		}

		private static async ValueTask<CardRequirement> AddHenchmanCardIds(CardRequirement requirement, int currentGamePackageId, GameServiceClient client)
		{
			Henchman henchmanInGamePackage = null;

			while (henchmanInGamePackage == null)
			{
				var itemName = ConsoleUtility.GetUserInput("Henchman Name: ");

				var henchmen = await HenchmanUtility.GetHenchmenAsync(client, name: itemName, nameMatchStyle: NameMatchStyle.Similar);

				if (henchmen.Count == 1)
					henchmanInGamePackage = henchmen.First();

				if (henchmen.Count == 0)
					ConsoleUtility.WriteLine($"Henchman '{itemName}' was not found.");

				if (henchmen.Count > 1)
				{
					henchmanInGamePackage = henchmen.FirstOrDefault(x => x.GamePackageId == currentGamePackageId);
					if (henchmanInGamePackage == null)
						ConsoleUtility.WriteLine($"Too many Henchmen match '{itemName}' and none were in the current GamePackage.");
				}
			}

			requirement.RequiredSetId = henchmanInGamePackage.Id;
			requirement.CardSetType = CardSetType.CardSetHenchman;
			ConsoleUtility.WriteLine($"Adding specific requirement for Henchman: {henchmanInGamePackage}");

			return requirement;
		}

		private static async ValueTask<CardRequirement> AddMastermindCardIds(CardRequirement requirement, int currentGamePackageId, GameServiceClient client)
		{
			Mastermind mastermindInGamePackage = null;

			while (mastermindInGamePackage == null)
			{
				var itemName = ConsoleUtility.GetUserInput("Mastermind Name: ");

				var masterminds = await MastermindUtility.GetMastermindsAsync(client, name: itemName, nameMatchStyle: NameMatchStyle.Similar);

				if (masterminds.Count == 1)
					mastermindInGamePackage = masterminds.First();

				if (masterminds.Count == 0)
					ConsoleUtility.WriteLine($"Mastermind '{itemName}' was not found.");

				if (masterminds.Count > 1)
				{
					mastermindInGamePackage = masterminds.FirstOrDefault(x => x.GamePackageId == currentGamePackageId);
					if (mastermindInGamePackage == null)
						ConsoleUtility.WriteLine($"Too many Masterminds match '{itemName}' and none were in the current GamePackage.");
				}
			}

			requirement.RequiredSetId = mastermindInGamePackage.Id;
			requirement.CardSetType = CardSetType.CardSetMastermind;
			ConsoleUtility.WriteLine($"Adding specific requirement for Mastermind: {mastermindInGamePackage}");

			return requirement;
		}

		private static async ValueTask<CardRequirement> AddNeutralCardIds(CardRequirement requirement, int currentGamePackageId, GameServiceClient client)
		{
			Neutral neutralInGamePackage = null;

			while (neutralInGamePackage == null)
			{
				var itemName = ConsoleUtility.GetUserInput("Neutral Name: ");

				var neutral = await NeutralUtility.GetNeutralsAsync(client, name: itemName, nameMatchStyle: NameMatchStyle.Similar);

				if (neutral.Count >= 1)
					neutralInGamePackage = neutral.First();

				if (neutral.Count == 0)
					ConsoleUtility.WriteLine($"Neutral '{itemName}' was not found.");
			}

			requirement.RequiredSetId = neutralInGamePackage.Id;
			requirement.CardSetType = CardSetType.CardSetNeutral;
			ConsoleUtility.WriteLine($"Adding specific requirement for Neutral: {neutralInGamePackage}");

			return requirement;
		}
	}
}