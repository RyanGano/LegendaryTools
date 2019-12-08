using System;
using System.Threading.Tasks;
using LegendaryService;
using Grpc.Net.Client;
using System.Linq;
using static LegendaryService.GameService;

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
				Console.Write("What do you want to do? ('h' for Help): ");
				var input = Console.ReadLine();

				appStatus = HandleInput(input, client);
			}

			return;
		}

		private static AppStatus HandleInput(string input, GameServiceClient client)
		{
			input = input?.ToLower() ?? "h";

			Func<GameServiceClient, AppStatus> handler = input switch
			{
				"h" => (GameServiceClient client) => { WriteHelp(); return AppStatus.Continue; },
				"help" => (GameServiceClient client) => { WriteHelp(); return AppStatus.Continue; },
				"gp" => (GameServiceClient client) => { DisplayGamePackagesAsync(client).Wait(); return AppStatus.Continue; },
				"gamepackages" => (GameServiceClient client) => { DisplayGamePackagesAsync(client).Wait(); return AppStatus.Continue; },
				"a" => (GameServiceClient client) => { DisplayAbilitiesAsync(client).Wait(); return AppStatus.Continue; },
				"abilities" => (GameServiceClient client) => { DisplayAbilitiesAsync(client).Wait(); return AppStatus.Continue; },
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

		private static async Task DisplayAbilitiesAsync(GameServiceClient client)
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

		private static async Task DisplayGamePackagesAsync(GameServiceClient client)
		{
			var request = new GetGamePackagesRequest();
			request.Fields.AddRange(new[] { GamePackageField.Id });
			var reply = await client.GetGamePackagesAsync(request);
			if (reply.Status.Code != 200)
				Console.WriteLine(reply.Status.Message);

			foreach (var package in reply.Packages)
			{
				var packageRequest = new GetGamePackageRequest();
				packageRequest.GamePackageId = package.Id;
				packageRequest.Fields.AddRange(new[] { GamePackageField.Id, GamePackageField.Name , GamePackageField.PackageType, GamePackageField.BaseMap });
				var packageReply = await client.GetGamePackageAsync(packageRequest);
				if (packageReply.Status.Code != 200)
					Console.WriteLine(packageReply.Status.Message);

				Console.WriteLine(packageReply.Package);
			}
		}

		private static void WriteHelp()
		{
			Console.WriteLine("Legendary Client");
			Console.WriteLine("");
			Console.WriteLine("Use this client to get information about cards in the Legendary Deck Building Game");
			Console.WriteLine("");
			Console.WriteLine("  help (h) - Display command help.");
			Console.WriteLine("  abilities (a) - Display all abilities.");
			Console.WriteLine("  gamepackages (gp) - Display all Game Packages.");
			Console.WriteLine("  quit (q) - Quit application.");
			Console.WriteLine("");
		}
	}
}