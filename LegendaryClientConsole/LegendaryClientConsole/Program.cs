using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using static LegendaryService.GameService;
using LegendaryClientConsole.Utility;

namespace LegendaryClientConsole
{
	class Program
	{
		static int Main(string[] args)
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

			return 0;
		}

		private static AppStatus HandleInput(string input, GameServiceClient client)
		{
			input = input?.ToLower() ?? "h";

			var splitInput = input.Split(" ");

			Func<GameServiceClient, AppStatus> handler = splitInput[0] switch
			{
				"?" => (GameServiceClient client) => { WriteHelp(); return AppStatus.Continue; },
				"help" => (GameServiceClient client) => { WriteHelp(); return AppStatus.Continue; },
				"gp" => (GameServiceClient client) => { GamePackageUtility.DisplayGamePackagesAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"gamepackages" => (GameServiceClient client) => { GamePackageUtility.DisplayGamePackagesAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"a" => (GameServiceClient client) => { AbilityUtility.DisplayAbilitiesAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"abilities" => (GameServiceClient client) => { AbilityUtility.DisplayAbilitiesAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"t" => (GameServiceClient client) => { TeamUtility.DisplayTeamsAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"teams" => (GameServiceClient client) => { TeamUtility.DisplayTeamsAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"n" => (GameServiceClient client) => { NeutralUtility.DisplayNeutralsAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"neutrals" => (GameServiceClient client) => { NeutralUtility.DisplayNeutralsAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"cl" => (GameServiceClient client) => { ClassUtility.DisplayClassesAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"classes" => (GameServiceClient client) => { ClassUtility.DisplayClassesAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"h" => (GameServiceClient client) => { HenchmanUtility.DisplayHenchmenAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"henchmen" => (GameServiceClient client) => { HenchmanUtility.DisplayHenchmenAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"ad" => (GameServiceClient client) => { AdversaryUtility.DisplayAdversariesAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"adversaries" => (GameServiceClient client) => { AdversaryUtility.DisplayAdversariesAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"c" => (GameServiceClient client) => { CreateUtility.CreateItemAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
				"create" => (GameServiceClient client) => { CreateUtility.CreateItemAsync(client, splitInput[1..]).Wait(); return AppStatus.Continue; },
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
			ConsoleUtility.WriteLine("  neutrals (n) [id/name] - Display all neutrals (or limit to id/name matches).");
			ConsoleUtility.WriteLine("  classes (cl) [id/name] - Display all classes (or limit to id/name matches).");
			ConsoleUtility.WriteLine("  henchmen (h) [id/name] - Display all henchmen (or limit to id/name matches).");
			ConsoleUtility.WriteLine("  adversaries (ad) [id/name] - Display all adversaries (or limit to id/name matches).");
			ConsoleUtility.WriteLine("  create (c) t|h|ad|n - Create a new team|henchman|adversary|neutral.");
			ConsoleUtility.WriteLine("  quit (q) - Quit application.");
			ConsoleUtility.WriteLine("");
		}
	}
}