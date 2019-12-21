using System;
using System.Threading.Tasks;
using LegendaryService;
using Grpc.Net.Client;
using System.Linq;
using static LegendaryService.GameService;
using System.Collections.Generic;

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
				string input = GetUserInput("What do you want to do? ('h' for Help): ");

				appStatus = HandleInput(input, client);
			}

			return;
		}

		private static string GetUserInput(string message)
		{
			Console.Write(message);
			return Console.ReadLine();
		}

		private static AppStatus HandleInput(string input, GameServiceClient client)
		{
			input = input?.ToLower() ?? "h";

			var splitInput = input.Split(" ");

			Func<GameServiceClient, AppStatus> handler = splitInput[0] switch
			{
				"h" => (GameServiceClient client) => { WriteHelp(); return AppStatus.Continue; },
				"help" => (GameServiceClient client) => { WriteHelp(); return AppStatus.Continue; },
				"gp" => (GameServiceClient client) => { DisplayGamePackagesAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"gamepackages" => (GameServiceClient client) => { DisplayGamePackagesAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"a" => (GameServiceClient client) => { DisplayAbilitiesAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"abilities" => (GameServiceClient client) => { DisplayAbilitiesAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"t" => (GameServiceClient client) => { DisplayTeamsAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"teams" => (GameServiceClient client) => { DisplayTeamsAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
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
			Console.WriteLine("'Initializing Database'");
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
					Console.WriteLine($"{ability.GamePackage.Name} - {ability.Name}");
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
				Console.WriteLine($"{ability.GamePackage.Name} - {ability.Name} - {ability.Description}");
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
				Console.WriteLine(reply.Status.Message);

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
				Console.WriteLine(packagesReply.Status.Message);

			foreach (var gamePackage in packagesReply.Packages)
				Console.WriteLine(gamePackage);
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
					Console.WriteLine($"{team}");
		}

		private static async Task CreateItemAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				Console.WriteLine("Must supply the type of item you want to create. (t)");
			else if (args.FirstOrDefault() == "t")
				await CreateTeamAsync(client);
		}

		private static async Task CreateTeamAsync(GameServiceClient client)
		{
			var teamName = GetUserInput("Team Name: ");
			var imagePath = GetUserInput("Path to Image (on OneDrive): ");

			var createRequest = new CreateTeamsRequest();
			createRequest.Teams.Add(new Team { Name = teamName, ImagePath = imagePath });
			createRequest.CreateOptions.Add(CreateOptions.ErrorOnDuplicates);

			var reply = await client.CreateTeamsAsync(createRequest);

			if (reply.Status.Code != 200)
				Console.WriteLine(reply.Status.Message);
			else
				Console.WriteLine($"Team {reply.Teams.First().Name} was created with Id {reply.Teams.First().Id}");
		}

		private static void WriteHelp()
		{
			Console.WriteLine("Legendary Client");
			Console.WriteLine("");
			Console.WriteLine("Use this client to get information about cards in the Legendary Deck Building Game");
			Console.WriteLine("");
			Console.WriteLine("  help (h) - Display command help.");
			Console.WriteLine("  abilities (a) [id/name] - Display all abilities (or limit to id/name matches).");
			Console.WriteLine("  gamepackages (gp) [id/name] - Display all Game Packages (or limit to id/name matches).");
			Console.WriteLine("  teams (t) [id/name] - Display all teams (or limit to id/name matches).");
			Console.WriteLine("  create (c) t - Create a new team.");
			Console.WriteLine("  quit (q) - Quit application.");
			Console.WriteLine("");
		}
	}
}