using Faithlife.Utility;
using LegendaryService;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static LegendaryService.GameService;

namespace LegendaryClientConsole.Utility
{
	public static class AbilityUtility
	{
		public static async Task DisplayAbilitiesAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				await DisplayAllAbilitiesAsync(client);
			else if (int.TryParse(args.FirstOrDefault(), out int id))
				await DisplayAbilitiesAsync(client, args.Select(x => int.Parse(x)).ToList(), null);
			else
				await DisplayAbilitiesAsync(client, null, args.FirstOrDefault());
		}

		public static async Task DisplayAllAbilitiesAsync(GameServiceClient client)
		{
			var request = new GetGamePackagesRequest();
			request.Fields.AddRange(new[] { GamePackageField.Id, GamePackageField.Name, GamePackageField.PackageType, GamePackageField.BaseMap });
			var reply = await client.GetGamePackagesAsync(request);

			foreach (var gameData in reply.Packages.OrderBy(x => x.BaseMap).ThenBy(x => x.PackageType).ThenBy(x => x.Name))
			{
				var abilitiesRequest = new GetAbilitiesRequest
				{
					GamePackageId = gameData.Id
				};

				abilitiesRequest.AbilityFields.AddRange(new[] { AbilityField.Id, AbilityField.Name, AbilityField.GamePackageName });

				var abilities = await client.GetAbilitiesAsync(abilitiesRequest);

				foreach (var ability in abilities.Abilities)
					ConsoleUtility.WriteLine($"{ability.GamePackage.Name} - {ability.Name}");
			}
		}

		public static async Task DisplayAbilitiesAsync(GameServiceClient client, IReadOnlyList<int> ids, string name)
		{
			var abilitiesRequest = new GetAbilitiesRequest();
			if (ids != null && ids.Count() != 0)
				abilitiesRequest.AbilityIds.AddRange(ids);
			else if (name != null)
				abilitiesRequest.Name = name;

			abilitiesRequest.AbilityFields.AddRange(new[] { AbilityField.Id, AbilityField.Name, AbilityField.Description, AbilityField.GamePackageName });

			var abilities = await client.GetAbilitiesAsync(abilitiesRequest);

			foreach (var ability in abilities.Abilities)
				ConsoleUtility.WriteLine($"{ability.GamePackage.Name} - {ability.Name} - {ability.Description}");
		}


		public static async ValueTask<IReadOnlyList<int>> SelectAbilityIds(GameServiceClient client)
		{
			List<int> abilityIds = new List<int>();
			IReadOnlyList<Ability> abilities = null;

			while (true)
			{
				int abilityId = 0;

				var input = ConsoleUtility.GetUserInput("What ability is this entry associated with (? to see listing, empty to finish): ");

				if (input == "")
				{
					if (ConsoleUtility.ShouldContinue($"Adding entry to abilities [{abilityIds.Select(x => x.ToString()).Join(", ")}]:"))
						return abilityIds;

					return await SelectAbilityIds(client);
				}

				if (input == "?")
				{
					abilities = await DisplayAbilitiesAsync(client, abilities);
				}
				else if (!string.IsNullOrWhiteSpace(input))
				{
					abilities = await GetAbilitiesAsync(client, abilities);

					if (int.TryParse(input, out int id))
					{
						abilityId = abilities.Select(x => x.Id).FirstOrDefault(x => x == id, 0);
						if (abilityId == 0)
							ConsoleUtility.WriteLine($"Ability Id '{input}' was not found");
					}
					else
					{
						var matchingAbilities = abilities.Where(x => Regex.IsMatch(x.Name.ToLower(), input.ToLower())).ToList();
						if (matchingAbilities.Count == 0)
							ConsoleUtility.WriteLine($"Ability Name '{input}' was not found");
						else if (matchingAbilities.Count != 1)
							ConsoleUtility.WriteLine($"Ability Name '{input}' matched multiple Abilities({matchingAbilities.Select(x => x.Name).Join(", ")})");
						else
							abilityId = matchingAbilities.First().Id;
					}
				}

				if (abilityId != 0)
				{
					var ability = abilities.First(x => x.Id == abilityId);
					if (ConsoleUtility.ShouldContinue($"Adding entry to ability '{ability.Id}: {ability.Name}':"))
						abilityIds.Add(abilityId);
				}
			}
		}

		public static async ValueTask<IReadOnlyList<Ability>> DisplayAbilitiesAsync(GameServiceClient client, IReadOnlyList<Ability> abilities)
		{
			abilities = await GetAbilitiesAsync(client, abilities);

			foreach (var ability in abilities)
				ConsoleUtility.WriteLine($"{ability.Id}: {ability.Name}");

			return abilities;
		}

		public static async Task<IReadOnlyList<Ability>> GetAbilitiesAsync(GameServiceClient client, IReadOnlyList<Ability> abilities)
		{
			if (abilities == null)
			{
				var request = new GetAbilitiesRequest();
				request.AbilityFields.AddRange(new[] { AbilityField.Id, AbilityField.Name });
				abilities = (await client.GetAbilitiesAsync(request)).Abilities.ToList();
			}

			return abilities;
		}
	}
}
