using Faithlife.Data;
using Google.Protobuf.Collections;
using Grpc.Core;
using LegendaryService.Database;
using LegendaryService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LegendaryService.Utility
{
	public class GamePackageUtility
	{
		public static async Task<GetGamePackagesReply> GetGamePackagesAsync(GetGamePackagesRequest request, ServerCallContext context)
		{
			var reply = new GetGamePackagesReply();

			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var select = DatabaseDefinition.BuildSelectStatement(request.Fields);
			var joins = DatabaseDefinition.BuildRequiredJoins(request.Fields);
			var where = request.GamePackageIds.Count() != 0 ?
				$"where { DatabaseDefinition.BuildWhereStatement(GamePackageField.Id, WhereStatementType.Includes)}" :
				!string.IsNullOrWhiteSpace(request.Name) ?
					$"where { DatabaseDefinition.BuildWhereStatement(GamePackageField.Name, WhereStatementType.Like)}" :
					"";
			var whereMatch = request.GamePackageIds.Count() != 0 ?
				new (string, object)[] { (DatabaseDefinition.GetSelectResult(GamePackageField.Id), request.GamePackageIds.ToArray()) } :
				!string.IsNullOrWhiteSpace(request.Name) ?
					new (string, object)[] { (DatabaseDefinition.GetSelectResult(GamePackageField.Name), $"%{request.Name}%") } :
					new (string, object)[] { };

			var gamePackages = await db.RunCommand(connector,
				$@"select {select} from {DatabaseDefinition.DefaultTableName} {joins} {where};",
				whereMatch,
				x => MapGamePackage(request.Fields, x));

			if (request.Fields.Contains(GamePackageField.Abilities))
			{
				IReadOnlyList<Ability> abilities = await AbilityUtility.GetAllAbilitiesAsync(gamePackages, connector);

				foreach (var gamePackage in gamePackages)
					gamePackage.AbilityIds.AddRange(abilities.Where(x => x.GamePackage.Id == gamePackage.Id).Select(x => x.Id));
			}

			if (gamePackages.Count() != 0)
				reply.Packages.AddRange(gamePackages);
			reply.Status = new Status { Code = 200 };

			return reply;
		}

		public static async Task<CreateGamePackageReply> CreateGamePackageAsync(CreateGamePackageRequest request, ServerCallContext context)
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
				("CoverImage", request.CoverImage),
				("PackageTypeId", packageTypeId),
				("BaseMapId", baseMapId)
				).QuerySingleOrDefaultAsync<int?>();

			gamePackageId = await connector.Command(@"select GamePackageId from gamepackages where name like @Name", ("Name", request.Name)).QuerySingleAsync<int>();

			if (!gamePackageId.HasValue)
				return new CreateGamePackageReply { Status = new Status { Code = 500, Message = "Item not added to database" } };

			return new CreateGamePackageReply { Id = gamePackageId.Value, Status = new Status { Code = 200 } };
		}

		private static GamePackage MapGamePackage(RepeatedField<GamePackageField> fields, IDataRecord x)
		{
			var gamePackage = new GamePackage();
			if (fields.Contains(GamePackageField.Id))
				gamePackage.Id = x.Get<int>(DatabaseDefinition.GetSelectResult(GamePackageField.Id));
			if (fields.Contains(GamePackageField.Name))
				gamePackage.Name = x.Get<string>(DatabaseDefinition.GetSelectResult(GamePackageField.Name));
			if (fields.Contains(GamePackageField.CoverImage))
				gamePackage.CoverImage = x.Get<string>(DatabaseDefinition.GetSelectResult(GamePackageField.CoverImage));
			if (fields.Contains(GamePackageField.PackageType))
				gamePackage.PackageType = (GamePackageType)Enum.Parse(typeof(GamePackageType), x.Get<string>(DatabaseDefinition.GetSelectResult(GamePackageField.PackageType)));
			if (fields.Contains(GamePackageField.BaseMap))
				gamePackage.BaseMap = (GameBaseMap)Enum.Parse(typeof(GameBaseMap), x.Get<string>(DatabaseDefinition.GetSelectResult(GamePackageField.BaseMap)));

			return gamePackage;
		}

		public static IDatabaseDefinition<GamePackageField> DatabaseDefinition = new GamePackageDatabaseDefinition();
	}
}
