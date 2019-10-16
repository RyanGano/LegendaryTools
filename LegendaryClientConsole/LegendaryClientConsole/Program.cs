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

			if (init)
			{
				await DatabaseInitializer.InitializeDatabase(client);
				return;
			}

			var request = new GetGamePackagesRequest();
			request.Fields.AddRange(new[] { GamePackageField.Id, GamePackageField.Name, GamePackageField.CoverImage, GamePackageField.PackageType, GamePackageField.Allies });
			var reply = await client.GetGamePackagesAsync(request);

			foreach (var gameData in reply.Packages.OrderBy(x => x.BaseMap).ThenBy(x => x.PackageType).ThenBy(x => x.Name))
			{
				Console.WriteLine(gameData);
				// Console.WriteLine($"ID: {gameData.Id}");
				// Console.WriteLine($"Name: {gameData.Name}");
				// Console.WriteLine($"CoverImage: {gameData.CoverImage}");
				//Console.WriteLine($"Base Map: {gameData.BaseMap}");
				//Console.WriteLine($"Package Type: {gameData.PackageType}");
				// Console.WriteLine();
			}
			// Console.WriteLine("Available Packages: " + reply.Packages.Select(x => x.CoverImage).Aggregate((left, right) => left + ", " + right));
			Console.WriteLine("Press any key to exit...");
			Console.ReadKey();
		}
	}
}