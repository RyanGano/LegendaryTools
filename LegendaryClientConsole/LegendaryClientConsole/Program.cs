using System;
using System.Threading.Tasks;
using LegendaryService;
using Grpc.Net.Client;
using System.Linq;
using static LegendaryService.GameService;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Faithlife.Utility;
using LegendaryClientConsole.Utility;

namespace LegendaryClientConsole
{
	class Program
	{
		static async Task Main(string[] args)
		{
			// The port number(5001) must match the port of the gRPC server.
			var channel = GrpcChannel.ForAddress("https://localhost:5001");
			var client = new GameServiceClient(channel);

			AppStatus appStatus = AppStatus.Continue;

			while (appStatus == AppStatus.Continue)
			{
				string input = ConsoleUtility.GetUserInput("What do you want to do? ('?' for Help): ");

				appStatus = HandleInput(input, client);
			}

			return;
		}

		private static AppStatus HandleInput(string input, GameServiceClient client)
		{
			input = input?.ToLower() ?? "h";

			var splitInput = input.Split(" ");

			Func<GameServiceClient, AppStatus> handler = splitInput[0] switch
			{
				"?" => (GameServiceClient client) => { WriteHelp(); return AppStatus.Continue; },
				"help" => (GameServiceClient client) => { WriteHelp(); return AppStatus.Continue; },
				"gp" => (GameServiceClient client) => { DisplayGamePackagesAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"gamepackages" => (GameServiceClient client) => { DisplayGamePackagesAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"a" => (GameServiceClient client) => { DisplayAbilitiesAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"abilities" => (GameServiceClient client) => { DisplayAbilitiesAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"t" => (GameServiceClient client) => { DisplayTeamsAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"teams" => (GameServiceClient client) => { DisplayTeamsAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"h" => (GameServiceClient client) => { DisplayHenchmenAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"henchmen" => (GameServiceClient client) => { DisplayHenchmenAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"c" => (GameServiceClient client) => { CreateItemAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"create" => (GameServiceClient client) => { CreateItemAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"i" => (GameServiceClient client) => {	 InitializeDatabase(client).Wait(); return AppStatus.Continue; },
				"init" => (GameServiceClient client) => { InitializeDatabase(client).Wait(); return AppStatus.Continue; },
				"q" => (GameServiceClient client) => AppStatus.Quit,
				"quit" => (GameServiceClient client) => AppStatus.Quit,
				_ => (GameServiceClient client) => { WriteHelp(); return AppStatus.Continue; },
			};

			return handler?.Invoke(client) ?? AppStatus.Continue;
		}

		private static async Task InitializeDatabase(GameServiceClient client)
		{
			ConsoleUtility.WriteLine("'Initializing Database'");
			await DatabaseInitializer.InitializeDatabase(client);
		}

		private static async Task DisplayAbilitiesAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				await DisplayAllAbilitiesAsync(client);
			else if (int.TryParse(args.FirstOrDefault(), out int id))
				await DisplayAbilitiesAsync(client, args.Select(x => int.Parse(x)).ToList(), null);
			else
				await DisplayAbilitiesAsync(client, null, args.FirstOrDefault());
		}

		private static async Task DisplayAllAbilitiesAsync(GameServiceClient client)
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

		private static async Task DisplayAbilitiesAsync(GameServiceClient client, IReadOnlyList<int> ids, string name)
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

		private static async Task DisplayGamePackagesAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				await DisplayAllGamePackagesAsync(client);
			else if (int.TryParse(args.FirstOrDefault(), out int id))
				await DisplayGamePackageAsync(client, args.Select(x => int.Parse(x)).ToList(), null);
			else
				await DisplayGamePackageAsync(client, null, args.FirstOrDefault());
		}

		private static async Task DisplayAllGamePackagesAsync(GameServiceClient client)
		{
			var request = new GetGamePackagesRequest();
			request.Fields.AddRange(new[] { GamePackageField.Id });
			var reply = await client.GetGamePackagesAsync(request);
			if (reply.Status.Code != 200)
				ConsoleUtility.WriteLine(reply.Status.Message);

			foreach (var package in reply.Packages)
				await DisplayGamePackageAsync(client, new[] { package.Id }, null);
		}

		private static async Task DisplayGamePackageAsync(GameServiceClient client, IReadOnlyList<int> packageIds, string name)
		{
			var packagesRequest = new GetGamePackagesRequest();
			if (packageIds != null && packageIds.Count() != 0)
				packagesRequest.GamePackageIds.AddRange(packageIds);
			else if (!string.IsNullOrWhiteSpace(name))
				packagesRequest.Name = name;
			else
				throw new ArgumentException("Either 'packageId' or 'name' must be non-null");

			packagesRequest.Fields.AddRange(new[] { GamePackageField.Id, GamePackageField.Name, GamePackageField.PackageType, GamePackageField.BaseMap });
			var packagesReply = await client.GetGamePackagesAsync(packagesRequest);
			if (packagesReply.Status.Code != 200)
				ConsoleUtility.WriteLine(packagesReply.Status.Message);

			foreach (var gamePackage in packagesReply.Packages)
				ConsoleUtility.WriteLine(gamePackage.ToString());
		}

		private static async Task DisplayTeamsAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				await DisplayTeamsAsync(client);
			else if (int.TryParse(args.FirstOrDefault(), out int id))
				await DisplayTeamsAsync(client, teamIds:args.Select(x => int.Parse(x)).ToList());
			else
				await DisplayTeamsAsync(client, name:args.FirstOrDefault(), allowCloseNameMatches:true);
		}

		private static async Task DisplayTeamsAsync(GameServiceClient client, IReadOnlyList<int> teamIds = null, string name = null, bool allowCloseNameMatches = false)
		{
			var request = new GetTeamsRequest();

			if (teamIds != null && teamIds.Count() != 0)
				request.TeamIds.AddRange(teamIds);
			else if (!string.IsNullOrWhiteSpace(name))
				request.Name = name;

			request.AllowCloseNameMatches = allowCloseNameMatches;

			var teams = await client.GetTeamsAsync(request);

				foreach (var team in teams.Teams)
				ConsoleUtility.WriteLine($"{team}");
		}

		private static async Task DisplayHenchmenAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				await DisplayHenchmenAsync(client);
			else if (int.TryParse(args.FirstOrDefault(), out int id))
				await DisplayHenchmenAsync(client, henchmanIds: args.Select(x => int.Parse(x)).ToList());
			else
				await DisplayHenchmenAsync(client, name: args.FirstOrDefault(), allowCloseNameMatches: true);
		}

		private static async Task DisplayHenchmenAsync(GameServiceClient client, IReadOnlyList<int> henchmanIds = null, string name = null, bool allowCloseNameMatches = false)
		{
			var request = new GetHenchmenRequest();

			if (henchmanIds != null && henchmanIds.Count() != 0)
				request.HenchmenIds.AddRange(henchmanIds);
			else if (!string.IsNullOrWhiteSpace(name))
				request.Name = name;

			request.AllowCloseNameMatches = allowCloseNameMatches;

			var henchmen = await client.GetHenchmenAsync(request);

			foreach (var henchman in henchmen.Henchmen)
				ConsoleUtility.WriteLine($"{henchman}");
		}

		private static async Task CreateItemAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				ConsoleUtility.WriteLine("Must supply the type of item you want to create. (t|h)");
			else if (args.FirstOrDefault() == "t")
				await CreateTeamAsync(client);
			else if (args.FirstOrDefault() == "h")
				await CreateHenchmanAsync(client);
		}

		private static async Task CreateTeamAsync(GameServiceClient client)
		{
			var teamName = ConsoleUtility.GetUserInput("Team Name: ");
			var imagePath = ConsoleUtility.GetUserInput("Path to Image (on OneDrive): ");

			var createRequest = new CreateTeamsRequest();
			createRequest.Teams.Add(new Team { Name = teamName, ImagePath = imagePath });
			createRequest.CreateOptions.Add(CreateOptions.ErrorOnDuplicates);

			var reply = await client.CreateTeamsAsync(createRequest);

			if (reply.Status.Code != 200)
				ConsoleUtility.WriteLine(reply.Status.Message);
			else
				ConsoleUtility.WriteLine($"Team '{reply.Teams.First().Name}' was created with Id '{reply.Teams.First().Id}'");
		}

		private static async Task CreateHenchmanAsync(GameServiceClient client)
		{
			var henchman = new Henchman();
			henchman.Name = ConsoleUtility.GetUserInput("Henchman Name: ");
			henchman.GamePackageId = await GetGamePackageId(client);
			henchman.AbilityIds.AddRange(await GetAbilityIds(client));

			if (!ConsoleUtility.ShouldContinue($"Creating Henchman: '{henchman.Name}', in gamePackage '{henchman.GamePackageId}' with abilities [{henchman.AbilityIds.Select(x => x.ToString()).Join(", ")}]"))
			{
				await CreateHenchmanAsync(client);
				return;
			}

			var createRequest = new CreateHenchmenRequest();
			createRequest.Henchmen.Add(henchman);
			var createReply = await client.CreateHenchmenAsync(createRequest);
			
			if (createReply.Status.Code != 200)
				ConsoleUtility.WriteLine($"Failed to create henchman: {createReply.Status.Message}");
			else
				ConsoleUtility.WriteLine($"Team '{createReply.Henchmen.First().Name}' was created with Id '{createReply.Henchmen.First().Id}'");
		}

		private static async ValueTask<int> GetGamePackageId(GameServiceClient client)
		{
			var gamePackageId = 0;
			IReadOnlyList<GamePackage> gamePackages = null;

			while (gamePackageId == 0)
			{
				var input = ConsoleUtility.GetUserInput("What game package is this entry associated with (? to see listing): ");
				if (input == "?")
				{
					gamePackages = await DisplayGamePackagesAsync(client, gamePackages);
				}
				else if (!string.IsNullOrWhiteSpace(input))
				{
					gamePackages = await GetGamePackagesAsync(client, gamePackages);

					if (int.TryParse(input, out int id))
					{
						gamePackageId = gamePackages.Select(x => x.Id).FirstOrDefault(x => x == id, 0);
						if (gamePackageId == 0)
							ConsoleUtility.WriteLine($"Game Package Id '{input}' was not found");
					}
					else
					{
						var matchingGamePackages = gamePackages.Where(x => Regex.IsMatch(x.Name.ToLower(), input.ToLower())).ToList();
						if (matchingGamePackages.Count == 0)
							ConsoleUtility.WriteLine($"Game Package Name '{input}' was not found");
						else if (matchingGamePackages.Count != 1)
							ConsoleUtility.WriteLine($"Game Package Name '{input}' matched multiple GamePackages ({matchingGamePackages.Select(x => x.Name).Join(", ")})");
						else
							gamePackageId = matchingGamePackages.First().Id;
					}
				}

				if (gamePackageId != 0)
				{
					var gamePackage = gamePackages.First(x => x.Id == gamePackageId);
					if (!ConsoleUtility.ShouldContinue($"Adding entry to game package '{gamePackage.Id}: {gamePackage.Name}':"))
						gamePackageId = 0;
				}
			}

			return gamePackageId;
		}

		private static async ValueTask<IReadOnlyList<GamePackage>> DisplayGamePackagesAsync(GameServiceClient client, IReadOnlyList<GamePackage> gamePackages)
		{
			gamePackages = await GetGamePackagesAsync(client, gamePackages);

			foreach (var gamePackage in gamePackages)
				ConsoleUtility.WriteLine($"{gamePackage.Id}: {gamePackage.Name}");

			return gamePackages;
		}

		private static async Task<IReadOnlyList<GamePackage>> GetGamePackagesAsync(GameServiceClient client, IReadOnlyList<GamePackage> gamePackages)
		{
			if (gamePackages == null)
			{
				var request = new GetGamePackagesRequest();
				request.Fields.AddRange(new[] { GamePackageField.Id, GamePackageField.Name });
				gamePackages = (await client.GetGamePackagesAsync(request)).Packages.ToList();
			}

			return gamePackages;
		}

		private static async ValueTask<IReadOnlyList<int>> GetAbilityIds(GameServiceClient client)
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

					return await GetAbilityIds(client);
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

		private static async ValueTask<IReadOnlyList<Ability>> DisplayAbilitiesAsync(GameServiceClient client, IReadOnlyList<Ability> abilities)
		{
			abilities = await GetAbilitiesAsync(client, abilities);

			foreach (var ability in abilities)
				ConsoleUtility.WriteLine($"{ability.Id}: {ability.Name}");

			return abilities;
		}

		private static async Task<IReadOnlyList<Ability>> GetAbilitiesAsync(GameServiceClient client, IReadOnlyList<Ability> abilities)
		{
			if (abilities == null)
			{
				var request = new GetAbilitiesRequest();
				request.AbilityFields.AddRange(new[] { AbilityField.Id, AbilityField.Name });
				abilities = (await client.GetAbilitiesAsync(request)).Abilities.ToList();
			}

			return abilities;
		}

		private static void WriteHelp()
		{
			ConsoleUtility.WriteLine("Legendary Client");
			ConsoleUtility.WriteLine("");
			ConsoleUtility.WriteLine("Use this client to get information about cards in the Legendary Deck Building Game");
			ConsoleUtility.WriteLine("");
			ConsoleUtility.WriteLine("  help (?) - Display command help.");
			ConsoleUtility.WriteLine("  abilities (a) [id/name] - Display all abilities (or limit to id/name matches).");
			ConsoleUtility.WriteLine("  gamepackages (gp) [id/name] - Display all Game Packages (or limit to id/name matches).");
			ConsoleUtility.WriteLine("  teams (t) [id/name] - Display all teams (or limit to id/name matches).");
			ConsoleUtility.WriteLine("  henchmen (h) [id/name] - Display all henchmen (or limit to id/name matches).");
			ConsoleUtility.WriteLine("  create (c) t|h - Create a new team|henchman.");
			ConsoleUtility.WriteLine("  quit (q) - Quit application.");
			ConsoleUtility.WriteLine("");
		}
	}
}