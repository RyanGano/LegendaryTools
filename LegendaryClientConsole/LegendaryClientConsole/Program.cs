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

			bool init = false;
			// init = true;

			if (init)
			{
				await DatabaseInitializer.InitializeDatabase(client);
				return;
			}

			var request = new GetGamePackagesRequest();
			request.Fields.AddRange(new[] { GamePackageField.Id, GamePackageField.Name, GamePackageField.CoverImage, GamePackageField.PackageType, GamePackageField.Allies, GamePackageField.Abilities });
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

			// Console.WriteLine("Available Packages: " + reply.Packages.Select(x => x.CoverImage).Aggregate((left, right) => left + ", " + right));
			Console.WriteLine("Press any key to exit...");
			Console.ReadKey();
		}
	}
}