using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using LegendaryService.Models;
using Microsoft.Extensions.Logging;
using Faithlife.Utility;
using System;
using System.Collections.Generic;
using Google.Protobuf.Collections;
using Faithlife.Data;

namespace LegendaryService
{
	public class Game : GameService.GameServiceBase
	{
		private readonly ILogger<Game> _logger;

		public Game(ILogger<Game> logger)
		{
			_logger = logger;
		}

		public override async Task<GetGamePackagesReply> GetGamePackages(GetGamePackagesRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var gamePackages = await connector.Command(@"
				select gp.GamePackageId as Id, gp.Name, gp.CoverImage, pt.Name as PackageType, bm.Name as BaseMap
					from gamepackages as gp
						inner join packagetypes as pt on pt.PackageTypeId = gp.PackageTypeId
						inner join basemaps as bm on bm.BaseMapId = gp.BaseMapId;").QueryAsync<GamePackage>(
						x => new GamePackage
						{
							Id = x.Get<int>(0),
							Name = x.GetString(1),
							CoverImage = x.GetString(2),
							PackageType = (GamePackageType)Enum.Parse(typeof(GamePackageType), x.GetString(3)),
							BaseMap = (GameBaseMap)Enum.Parse(typeof(GameBaseMap), x.GetString(4))
						});

			var reply = new GetGamePackagesReply();
			if (gamePackages.Count() != 0)
				reply.Packages.AddRange(gamePackages);
			reply.Status = new Status { Code = 200 };

			return reply;
		}

		public override async Task<CreateGamePackageReply> CreateGamePackage(CreateGamePackageRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var gamePackageId = await connector.Command(@"select GamePackageId from gamepackages where name like @Name", ("Name", request.Name)).QuerySingleOrDefaultAsync<int?>();

			if (gamePackageId.HasValue)
				return new CreateGamePackageReply { Status = new Status { Code = 400, Message = "Game Package already in database" } };

			var packageTypeId = await connector.Command(@"select PackageTypeId from packagetypes where Name = @Name;", ("Name", request.PackageType.ToString())).QuerySingleAsync<int>();
			var baseMapId = await connector.Command(@"select BaseMapId from basemaps where Name = @Name;", ("Name", request.BaseMap.ToString())).QuerySingleAsync<int>();

			await connector.Command(@"
				insert into gamepackages (Name, CoverImage, PackageTypeId, BaseMapId) values (@Name, @CoverImage, @PackageTypeId, @BaseMapId)",
				("Name", request.Name),
				("", request.CoverImage),
				("PackageTypeId", packageTypeId),
				("BaseMapId", baseMapId)
				).QuerySingleOrDefaultAsync<int?>();

			gamePackageId = await connector.Command(@"select GamePackageId from gamepackages where name like @Name", ("Name", request.Name)).QuerySingleAsync<int>();

			if (!gamePackageId.HasValue)
				return new CreateGamePackageReply { Status = new Status { Code = 500, Message = "Item not added to database" } };

			return new CreateGamePackageReply { Id = gamePackageId.Value, Status = new Status { Code = 200 } };
		}

		public override async Task<CreateAbilitiesReply> CreateAbilities(CreateAbilitiesRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new CreateAbilitiesReply { Status = new Status { Code = 200 } };

			var groupedAbilities = request.Abilities.GroupBy(x => x.Name.ToLower());
			if (groupedAbilities.Any(x => x.Count() != 1 && x.DistinctBy(ability => ability.GamePackage.Id).Count() == 1))
			{
				reply.Status.Code = 400;
				reply.Status.Message = $"Duplicate entries in request: " + groupedAbilities.Where(x => x.Count() != 1).Select(x => x.Key).Join(", ");
				return reply;
			}

			var abilities = await connector.Command(@"select * from abilities where name REGEXP @Names;", ("Names", request.Abilities.Select(x => x.Name).Join("|"))).QueryAsync<Ability>();
			if (abilities.Count() != 0)
			{
				reply.Status.Message = $"Database already contains entries: " + abilities.Select(x => x.Name).Join(", ");

				if (request.CreateOptions.Contains(CreateOptions.ErrorOnDuplicates))
				{
					reply.Status.Code = 400;
					return reply;
				}
			}

			await connector.Command(@"insert into abilities (Name, Description, GamePackageId) values (@newAbilites);", ("newAbilites", request.Abilities.Where(x => !abilities.Any(existingAbility => existingAbility.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase))))).ExecuteAsync();

			abilities = await connector.Command(@"select * from abilities where name REGEXP @Names;", ("Names", request.Abilities.Select(x => x.Name).Join("|"))).QueryAsync<Ability>();

			reply.Abilities.AddRange(abilities);
			return reply;
		}

		readonly List<GamePackage> m_gamePackages = new List<GamePackage>();

		private GamePackage Map(GamePackage gameData, RepeatedField<GamePackageField> fields)
		{
			var gamePackage = new GamePackage();

			foreach (GamePackageField field in fields)
			{
				if (field == GamePackageField.Id)
					gamePackage.Id = gameData.Id;
				if (field == GamePackageField.Name)
					gamePackage.Name = gameData.Name;
				if (field == GamePackageField.CoverImage)
					gamePackage.CoverImage = gameData.CoverImage;
				if (field == GamePackageField.PackageType)
					gamePackage.PackageType = gameData.PackageType;
			}

			return gamePackage;
		}
	}
}
