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
using System.Data;
using System.Text;
using LegendaryService.Database;

namespace LegendaryService
{
	public class Game : GameService.GameServiceBase
	{
		private readonly ILogger<Game> _logger;

		public Game(ILogger<Game> logger)
		{
			_logger = logger;
			m_abilityDatabaseDefinition = new AbilityDatabaseDefinition();
			m_gamePackageDatabaseDefinition = new GamePackageDatabaseDefinition();
		}

		public override async Task<GetGamePackagesReply> GetGamePackages(GetGamePackagesRequest request, ServerCallContext context)
		{
			var reply = new GetGamePackagesReply();

			if (request.GamePackageIds.Count > 1)
			{
				reply.Status = new Status { Code = 400, Message = "Muliple Game Package Ids not supported yet." };
				return reply;
			}
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var select = m_gamePackageDatabaseDefinition.BuildSelectStatement(request.Fields);
			var joins = m_gamePackageDatabaseDefinition.BuildRequiredJoins(request.Fields);
			var where = (request.GamePackageIds?.Count() ?? 0) != 0 ? $"where { m_gamePackageDatabaseDefinition.BuildWhereStatement(GamePackageField.Id, WhereStatementType.Equals)}" : "";
			var whereMatch = ((request.GamePackageIds?.Count() ?? 0) != 0) ? new (string, object)[] { (m_gamePackageDatabaseDefinition.GetSelectResult(GamePackageField.Id), request.GamePackageIds.First()) } : new (string, object)[] { };
			
			var gamePackages = await db.RunCommand(connector,
				$@"select {select} from {m_gamePackageDatabaseDefinition.DefaultTableName} {joins} {where};",
				whereMatch, 
						x => 
						{
							var gamePackage = new GamePackage();
							if (request.Fields.Contains(GamePackageField.Id))
								gamePackage.Id = x.Get<int>(m_gamePackageDatabaseDefinition.GetSelectResult(GamePackageField.Id));
							if (request.Fields.Contains(GamePackageField.Name))
								gamePackage.Name = x.Get<string>(m_gamePackageDatabaseDefinition.GetSelectResult(GamePackageField.Name));
							if (request.Fields.Contains(GamePackageField.CoverImage))
								gamePackage.CoverImage = x.Get<string>(m_gamePackageDatabaseDefinition.GetSelectResult(GamePackageField.CoverImage));
							if (request.Fields.Contains(GamePackageField.PackageType))
								gamePackage.PackageType = (GamePackageType)Enum.Parse(typeof(GamePackageType), x.Get<string>(m_gamePackageDatabaseDefinition.GetSelectResult(GamePackageField.PackageType)));
							if (request.Fields.Contains(GamePackageField.BaseMap))
								gamePackage.BaseMap = (GameBaseMap)Enum.Parse(typeof(GameBaseMap), x.Get<string>(m_gamePackageDatabaseDefinition.GetSelectResult(GamePackageField.BaseMap)));

							return gamePackage;
						});

			if (request.Fields.Contains(GamePackageField.Abilities))
			{
				IReadOnlyList<Ability> abilities = await GetAllAbilities(gamePackages, connector);
				
				foreach (var gamePackage in gamePackages)
					gamePackage.AbilitieIds.AddRange(abilities.Where(x => x.GamePackage.Id == gamePackage.Id).Select(x => x.Id));
			}

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
				("CoverImage", request.CoverImage),
				("PackageTypeId", packageTypeId),
				("BaseMapId", baseMapId)
				).QuerySingleOrDefaultAsync<int?>();

			gamePackageId = await connector.Command(@"select GamePackageId from gamepackages where name like @Name", ("Name", request.Name)).QuerySingleAsync<int>();

			if (!gamePackageId.HasValue)
				return new CreateGamePackageReply { Status = new Status { Code = 500, Message = "Item not added to database" } };

			return new CreateGamePackageReply { Id = gamePackageId.Value, Status = new Status { Code = 200 } };
		}
		public override async Task<GetAbilitiesReply> GetAbilities(GetAbilitiesRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });
			var reply = new GetAbilitiesReply { Status = new Status { Code = 200 } };

			List<Ability> abilities = new List<Ability>();

			var select = m_abilityDatabaseDefinition.BuildSelectStatement(request.AbilityFields);
			var joins = m_abilityDatabaseDefinition.BuildRequiredJoins(request.AbilityFields);

			if (request.GamePackageId > 0)
				reply.Abilities.AddRange(await connector
					.Command($@"select {select} from {m_abilityDatabaseDefinition.DefaultTableName} {joins} where {m_abilityDatabaseDefinition.GetWhereStatement(AbilityField.GamePackageId)} = @{m_abilityDatabaseDefinition.ColumnName[AbilityField.GamePackageId]};",
						(m_abilityDatabaseDefinition.ColumnName[AbilityField.GamePackageId], request.GamePackageId))
					.QueryAsync(x => MapAbility(x, request.AbilityFields, null)));

			return reply;
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

			var existingAbilities = await GetExistingAbilities(request.Abilities, connector);
			if (existingAbilities.Count() != 0)
			{
				reply.Status.Message = $"Database already contains entries: " + existingAbilities.Select(x => x.Name).Join(", ");

				if (request.CreateOptions.Contains(CreateOptions.ErrorOnDuplicates))
				{
					reply.Status.Code = 400;
					return reply;
				}
			}

			var abilitiesToInsert = request.Abilities.Where(x => !existingAbilities.Any(existingAbility => existingAbility.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase))).Select(x => new { x.Name, x.Description, GamePackageId = x.GamePackage.Id }).ToList();

			using var transaction = await connector.BeginTransactionAsync();
			
			foreach (var abilityToInsert in abilitiesToInsert)
				await connector.Command(@"insert into abilities (Name, Description, GamePackageId) values (@Name, @Description, @GamePackageId);", DbParameters.FromDto(abilityToInsert)).ExecuteAsync();

			await connector.CommitTransactionAsync();

			existingAbilities = await GetExistingAbilities(request.Abilities, connector);

			reply.Abilities.AddRange(existingAbilities);
			return reply;
		}
		private async ValueTask<IReadOnlyList<Ability>> GetAllAbilities(IReadOnlyList<GamePackage> gamePackages, DbConnector connector)
		{
			var test = (await connector.Command($@"select {m_abilityDatabaseDefinition.BuildSelectStatement(null)} from abilities;").QueryAsync(x => MapAbility(x, m_abilityDatabaseDefinition.BasicFields, gamePackages))).ToList();
			return test;
		}

		private async ValueTask<IReadOnlyList<Ability>> GetExistingAbilities(RepeatedField<Ability> abilities, DbConnector connector)
		{
			var foundAbilities = new List<Ability>();

			var gamePackages = abilities.Select(x => x.GamePackage).DistinctBy(x => x.Id).ToList();

			foreach (var gamePackageAbilities in abilities.GroupBy(x => x.GamePackage.Id))
			{
				foundAbilities.AddRange(await connector.Command($@"select {m_abilityDatabaseDefinition.BuildSelectStatement(null)} from abilities where name REGEXP @Names and GamePackageId = @GamePackageId;",
					("Names", gamePackageAbilities.Select(x => LegendaryDatabase.Encode(x.Name)).Join("|")),
					("GamePackageId", gamePackageAbilities.Key))
					.QueryAsync(x => MapAbility(x, m_abilityDatabaseDefinition.BasicFields, gamePackages)));
			}

			return foundAbilities;
		}

		private Ability MapAbility(IDataRecord data, IReadOnlyList<AbilityField> fields, IReadOnlyList<GamePackage> gamePackages)
		{
			var gamePackage = gamePackages?.FirstOrDefault(x => x.Id == data.Get<int>(m_abilityDatabaseDefinition.GetSelectResult(AbilityField.GamePackageId)));

			if (gamePackage == null && (fields.Contains(AbilityField.GamePackageId) || fields.Contains(AbilityField.GamePackageName)))
			{
				gamePackage = new GamePackage();

				if (fields.Contains(AbilityField.GamePackageId))
					gamePackage.Id = data.Get<int>(m_abilityDatabaseDefinition.GetSelectResult(AbilityField.GamePackageId));

				if (fields.Contains(AbilityField.GamePackageName))
					gamePackage.Name = data.Get<string>(m_abilityDatabaseDefinition.GetSelectResult(AbilityField.GamePackageName));
			}


			return new Ability
			{
				Id = fields.Contains(AbilityField.Id) ? data.Get<int>(m_abilityDatabaseDefinition.GetSelectResult(AbilityField.Id)) : 0,
				Name = fields.Contains(AbilityField.Name) ? data.Get<string>(m_abilityDatabaseDefinition.GetSelectResult(AbilityField.Name)) : "",
				Description = fields.Contains(AbilityField.Description) ? data.Get<string>(m_abilityDatabaseDefinition.GetSelectResult(AbilityField.Description)) : "",
				GamePackage = gamePackage
			};
		}

		IDatabaseDefinition<AbilityField> m_abilityDatabaseDefinition;
		IDatabaseDefinition<GamePackageField> m_gamePackageDatabaseDefinition;
	}
}
