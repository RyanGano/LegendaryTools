using Faithlife.Utility;
using LegendaryService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static LegendaryService.GameService;

namespace LegendaryClientConsole.Utility
{
	public static class GamePackageUtility
	{

		public static async Task DisplayGamePackagesAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				await DisplayAllGamePackagesAsync(client);
			else if (int.TryParse(args.FirstOrDefault(), out int id))
				await DisplayGamePackageAsync(client, args.Select(x => int.Parse(x)).ToList(), null);
			else
				await DisplayGamePackageAsync(client, null, args.FirstOrDefault());
		}

		public static async Task DisplayAllGamePackagesAsync(GameServiceClient client)
		{
			var request = new GetGamePackagesRequest();
			request.Fields.AddRange(new[] { GamePackageField.Id });
			var reply = await client.GetGamePackagesAsync(request);
			if (reply.Status.Code != 200)
				ConsoleUtility.WriteLine(reply.Status.Message);

			foreach (var package in reply.Packages)
				await DisplayGamePackageAsync(client, new[] { package.Id }, null);
		}

		public static async Task DisplayGamePackageAsync(GameServiceClient client, IReadOnlyList<int> packageIds, string name)
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

		public static async ValueTask<int> SelectGamePackageId(GameServiceClient client)
		{
			var gamePackageId = 0;
			IReadOnlyList<GamePackage> gamePackages = null;

			while (gamePackageId == 0)
			{
				var input = ConsoleUtility.GetUserInput("What game package is this entry associated with (? to see listing): ");
				if (input == "?")
				{
					gamePackages = await DisplayGamePackagesSimpleAsync(client, gamePackages);
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

		public static async ValueTask<IReadOnlyList<GamePackage>> DisplayGamePackagesSimpleAsync(GameServiceClient client, IReadOnlyList<GamePackage> gamePackages)
		{
			gamePackages = await GetGamePackagesAsync(client, gamePackages);

			foreach (var gamePackage in gamePackages)
				ConsoleUtility.WriteLine($"{gamePackage.Id}: {gamePackage.Name}");

			return gamePackages;
		}

		public static async Task<IReadOnlyList<GamePackage>> GetGamePackagesAsync(GameServiceClient client, IReadOnlyList<GamePackage> gamePackages)
		{
			if (gamePackages == null)
			{
				var request = new GetGamePackagesRequest();
				request.Fields.AddRange(new[] { GamePackageField.Id, GamePackageField.Name });
				gamePackages = (await client.GetGamePackagesAsync(request)).Packages.ToList();
			}

			return gamePackages;
		}
	}
}
