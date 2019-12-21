using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using LegendaryService;
using static LegendaryService.GameService;

namespace LegendaryClientConsole
{
	internal class DatabaseInitializer
	{
		internal static async ValueTask InitializeDatabase(GameServiceClient client)
		{
			await CreateGamePackages(client);

			var request = new GetGamePackagesRequest();
			request.Fields.AddRange(new[] { GamePackageField.Id, GamePackageField.Name, GamePackageField.CoverImage, GamePackageField.PackageType, GamePackageField.Allies });
			var basePackages = await client.GetGamePackagesAsync(request);
			
			await CreateAbilities(client, basePackages.Packages.ToList());
		}

		private static async ValueTask CreateAbilities(GameServiceClient client, IReadOnlyList<GamePackage> packages)
		{
			Console.WriteLine("Creating abilities");
			var doc = XDocument.Load(@"C:\Users\Ryan\SkyDrive\code\LegendaryGameStarter\LegendaryGameModel2\Abilities\Abilities.xml");

			var request = new CreateAbilitiesRequest();
			request.Abilities.AddRange(doc.Root.Elements("Ability").Select(ability =>
			{
				var gamePackage = packages.First(x => x.Name.Equals(ability.Element("Source").Value, StringComparison.OrdinalIgnoreCase));
				return new Ability { Name = ability.Element("Name").Value, Description = ability.Element("Description").Value, GamePackage = gamePackage };
			}));

			var result = await client.CreateAbilitiesAsync(request);
			Console.WriteLine($"Status: {result.Status.Code}: {result.Status.Message}");
		}

		private static async ValueTask CreateGamePackages(GameServiceClient client)
		{
			var sets = new[]
			{
				new GamePackage { Name = "Legendary", PackageType = GamePackageType.BaseGame, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Legendary Marvel Studios", PackageType = GamePackageType.BaseGame, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Legendary Villains", PackageType = GamePackageType.BaseGame, BaseMap = GameBaseMap.Villains },

				new GamePackage { Name = "X-Men", PackageType = GamePackageType.LargeExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "World War Hulk", PackageType = GamePackageType.LargeExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Secret Wars Volume 1", PackageType = GamePackageType.LargeExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Secret Wars Volume 2", PackageType = GamePackageType.LargeExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Civil War", PackageType = GamePackageType.LargeExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Revelations", PackageType = GamePackageType.LargeExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Dark City", PackageType = GamePackageType.LargeExpansion, BaseMap = GameBaseMap.Legendary },

				new GamePackage { Name = "Venom", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Spider-Man Homecoming", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Paint the Town Red (Spider-Man)", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Noir", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Guardians of the Galaxy", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Fantastic Four", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Deadpool", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Champions", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Captain America 75th Anniversary", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Ant-Man", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Dimensions", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Fear Itself", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Villains },
			};

			foreach (var set in sets)
			{
				var createRequest = new CreateGamePackageRequest
				{
					Name = set.Name,
					PackageType = set.PackageType,
					BaseMap = set.BaseMap
				};

				var createResponse = await client.CreateGamePackageAsync(createRequest);

				Console.WriteLine($"{set.Name} - {createResponse.Id} : {createResponse.Status?.Code}");
			}
		}
	}
}