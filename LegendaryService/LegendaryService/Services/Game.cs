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
			m_teamDatabaseDefinition = new TeamDatabaseDefinition();
			m_classDatabaseDefinition = new ClassDatabaseDefinition();
			m_henchmanDatabaseDefinition = new HenchmanDatabaseDefinition();
			m_adversaryDatabaseDefinition = new AdversaryDatabaseDefinition();
		}

		public override async Task<GetGamePackagesReply> GetGamePackages(GetGamePackagesRequest request, ServerCallContext context)
		{
			var reply = new GetGamePackagesReply();

			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var select = m_gamePackageDatabaseDefinition.BuildSelectStatement(request.Fields);
			var joins = m_gamePackageDatabaseDefinition.BuildRequiredJoins(request.Fields);
			var where = request.GamePackageIds.Count() != 0 ?
				$"where { m_gamePackageDatabaseDefinition.BuildWhereStatement(GamePackageField.Id, WhereStatementType.Includes)}" :
				!string.IsNullOrWhiteSpace(request.Name) ?
					$"where { m_gamePackageDatabaseDefinition.BuildWhereStatement(GamePackageField.Name, WhereStatementType.Like)}" :
					"";
			var whereMatch = request.GamePackageIds.Count() != 0 ?
				new (string, object)[] { (m_gamePackageDatabaseDefinition.GetSelectResult(GamePackageField.Id), request.GamePackageIds.ToArray()) } :
				!string.IsNullOrWhiteSpace(request.Name) ?
					new (string, object)[] { (m_gamePackageDatabaseDefinition.GetSelectResult(GamePackageField.Name), $"%{request.Name}%") } :
					new (string, object)[] { };
			
			var gamePackages = await db.RunCommand(connector,
				$@"select {select} from {m_gamePackageDatabaseDefinition.DefaultTableName} {joins} {where};",
				whereMatch,
				x => MapGamePackage(request.Fields, x));

			if (request.Fields.Contains(GamePackageField.Abilities))
			{
				IReadOnlyList<Ability> abilities = await GetAllAbilities(gamePackages, connector);
				
				foreach (var gamePackage in gamePackages)
					gamePackage.AbilityIds.AddRange(abilities.Where(x => x.GamePackage.Id == gamePackage.Id).Select(x => x.Id));
			}

			if (gamePackages.Count() != 0)
				reply.Packages.AddRange(gamePackages);
			reply.Status = new Status { Code = 200 };

			return reply;
		}

		private GamePackage MapGamePackage(RepeatedField<GamePackageField> fields, IDataRecord x)
		{
			var gamePackage = new GamePackage();
			if (fields.Contains(GamePackageField.Id))
				gamePackage.Id = x.Get<int>(m_gamePackageDatabaseDefinition.GetSelectResult(GamePackageField.Id));
			if (fields.Contains(GamePackageField.Name))
				gamePackage.Name = x.Get<string>(m_gamePackageDatabaseDefinition.GetSelectResult(GamePackageField.Name));
			if (fields.Contains(GamePackageField.CoverImage))
				gamePackage.CoverImage = x.Get<string>(m_gamePackageDatabaseDefinition.GetSelectResult(GamePackageField.CoverImage));
			if (fields.Contains(GamePackageField.PackageType))
				gamePackage.PackageType = (GamePackageType)Enum.Parse(typeof(GamePackageType), x.Get<string>(m_gamePackageDatabaseDefinition.GetSelectResult(GamePackageField.PackageType)));
			if (fields.Contains(GamePackageField.BaseMap))
				gamePackage.BaseMap = (GameBaseMap)Enum.Parse(typeof(GameBaseMap), x.Get<string>(m_gamePackageDatabaseDefinition.GetSelectResult(GamePackageField.BaseMap)));

			return gamePackage;
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

			var select = m_abilityDatabaseDefinition.BuildSelectStatement(request.AbilityFields);
			var joins = m_abilityDatabaseDefinition.BuildRequiredJoins(request.AbilityFields);
			var where = request.GamePackageId != 0 ?
				$"where { m_abilityDatabaseDefinition.BuildWhereStatement(AbilityField.GamePackageId, WhereStatementType.Equals)}" :
				request.AbilityIds.Count() != 0 ?
					$"where { m_abilityDatabaseDefinition.BuildWhereStatement(AbilityField.Id, WhereStatementType.Includes)}" :
					!string.IsNullOrWhiteSpace(request.Name) ?
						$"where { m_abilityDatabaseDefinition.BuildWhereStatement(AbilityField.Name, WhereStatementType.Like)}" :
						"";
			
			var whereMatch = request.GamePackageId != 0 ?
				new (string, object)[] { (m_abilityDatabaseDefinition.GetSelectResult(AbilityField.GamePackageId), request.GamePackageId) } :
				request.AbilityIds.Count() != 0 ?
					new (string, object)[] { (m_abilityDatabaseDefinition.GetSelectResult(AbilityField.Id), request.AbilityIds.ToArray()) } :
					!string.IsNullOrWhiteSpace(request.Name) ?
						new (string, object)[] { (m_abilityDatabaseDefinition.GetSelectResult(AbilityField.Name), $"%{request.Name}%") } :
						new (string, object)[] { };

			reply.Abilities.AddRange(await db.RunCommand(connector,
				$@"select {select} from {m_abilityDatabaseDefinition.DefaultTableName} {joins} {where};",
				whereMatch,
				x => MapAbility(x, request.AbilityFields, null)));

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

		public override async Task<GetTeamsReply> GetTeams(GetTeamsRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new GetTeamsReply { Status = new Status { Code = 200 } };

			var select = m_teamDatabaseDefinition.BuildSelectStatement(request.Fields);
			var joins = m_teamDatabaseDefinition.BuildRequiredJoins(request.Fields);

			var where = !string.IsNullOrWhiteSpace(request.Name) ?
					$"where { m_teamDatabaseDefinition.BuildWhereStatement(TeamField.TeamName, WhereStatementType.Like)}" :
					request.TeamIds.Count() != 0 ?
						$"where { m_teamDatabaseDefinition.BuildWhereStatement(TeamField.TeamId, WhereStatementType.Includes)}" :
						"";

			var whereMatch = !string.IsNullOrWhiteSpace(request.Name) ?
					new (string, object)[] { (m_teamDatabaseDefinition.GetSelectResult(TeamField.TeamName), $"%{request.Name}%") } :
					request.TeamIds.Count() != 0 ?
						new (string, object)[] { (m_teamDatabaseDefinition.GetSelectResult(TeamField.TeamId), request.TeamIds.ToArray()) } :
						new (string, object)[] { };

			reply.Teams.AddRange(await db.RunCommand(connector,
				$@"select {select} from {m_teamDatabaseDefinition.DefaultTableName} {joins} {where};",
				whereMatch,
				x => MapTeam(x, request.Fields)));

			return reply;
		}

		public override async Task<CreateTeamsReply> CreateTeams(CreateTeamsRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new CreateTeamsReply { Status = new Status { Code = 200 } };

			var teams = await GetTeams(new GetTeamsRequest(), context);

			var teamsToAdd = request.Teams.Where(x => !teams.Teams.Any(y => y.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase))).ToList();

			if (teamsToAdd.Count != request.Teams.Count() && request.CreateOptions.Contains(CreateOptions.ErrorOnDuplicates))
			{
				reply.Status.Code = 400;
				reply.Status.Message = $"Cannot add duplicate teams ({request.Teams.Except(teamsToAdd).Select(x => x.Name).Join(", ")}).";
				return reply;
			}

			List<int> insertIds = request.Teams.Except(teamsToAdd).Select(x => x.Id).ToList();

			foreach (var team in teamsToAdd)
			{
				insertIds.Add((int)(await connector.Command($@"
					insert
						into {m_teamDatabaseDefinition.DefaultTableName}
							({m_teamDatabaseDefinition.ColumnName[TeamField.TeamName]}, {m_teamDatabaseDefinition.ColumnName[TeamField.TeamImagePath]})
						values (@TeamName, @ImagePath);
					select last_insert_id();",
					("TeamName", team.Name),
					("ImagePath", team.ImagePath))
					.QuerySingleAsync<ulong>()));
			}

			var finalTeamsList = new GetTeamsRequest();
			finalTeamsList.TeamIds.AddRange(insertIds);
			var createdTeams = await GetTeams(finalTeamsList, context);

			reply.Teams.AddRange(createdTeams.Teams);
			return reply;
		}

		private Team MapTeam(IDataRecord data, IReadOnlyList<TeamField> fields)
		{
			var team = new Team();

			if (fields.Count == 0)
				fields = m_teamDatabaseDefinition.BasicFields;

			if (fields.Contains(TeamField.TeamId))
				team.Id = data.Get<int>(m_teamDatabaseDefinition.GetSelectResult(TeamField.TeamId));
			if (fields.Contains(TeamField.TeamName))
				team.Name = data.Get<string>(m_teamDatabaseDefinition.GetSelectResult(TeamField.TeamName));
			if (fields.Contains(TeamField.TeamImagePath))
				team.ImagePath = data.Get<string>(m_teamDatabaseDefinition.GetSelectResult(TeamField.TeamImagePath));

			return team;
		}

		public override async Task<GetClassesReply> GetClasses(GetClassesRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new GetClassesReply { Status = new Status { Code = 200 } };

			var select = m_classDatabaseDefinition.BuildSelectStatement(request.Fields);
			var joins = m_classDatabaseDefinition.BuildRequiredJoins(request.Fields);

			var where = !string.IsNullOrWhiteSpace(request.Name) ?
					$"where { m_classDatabaseDefinition.BuildWhereStatement(ClassField.ClassName, WhereStatementType.Like)}" :
					request.ClassIds.Count() != 0 ?
						$"where { m_classDatabaseDefinition.BuildWhereStatement(ClassField.ClassId, WhereStatementType.Includes)}" :
						"";

			var whereMatch = !string.IsNullOrWhiteSpace(request.Name) ?
					new (string, object)[] { (m_classDatabaseDefinition.GetSelectResult(ClassField.ClassName), $"%{request.Name}%") } :
					request.ClassIds.Count() != 0 ?
						new (string, object)[] { (m_classDatabaseDefinition.GetSelectResult(ClassField.ClassId), request.ClassIds.ToArray()) } :
						new (string, object)[] { };

			reply.Classes.AddRange(await db.RunCommand(connector,
				$@"select {select} from {m_classDatabaseDefinition.DefaultTableName} {joins} {where};",
				whereMatch,
				x => MapClass(x, request.Fields)));

			return reply;
		}

		public override async Task<CreateClassesReply> CreateClasses(CreateClassesRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new CreateClassesReply { Status = new Status { Code = 200 } };

			var classes = await GetClasses(new GetClassesRequest(), context);

			var classesToAdd = request.Classes.Where(x => !classes.Classes.Any(y => y.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase))).ToList();

			if (classesToAdd.Count != request.Classes.Count() && request.CreateOptions.Contains(CreateOptions.ErrorOnDuplicates))
			{
				reply.Status.Code = 400;
				reply.Status.Message = $"Cannot add duplicate classes ({request.Classes.Except(classesToAdd).Select(x => x.Name).Join(", ")}).";
				return reply;
			}

			List<int> insertIds = request.Classes.Except(classesToAdd).Select(x => x.Id).ToList();

			foreach (var @class in classesToAdd)
			{
				insertIds.Add((int)(await connector.Command($@"
					insert
						into {m_classDatabaseDefinition.DefaultTableName}
							({m_classDatabaseDefinition.ColumnName[ClassField.ClassName]}, {m_classDatabaseDefinition.ColumnName[ClassField.ClassImagePath]})
						values (@ClassName, @ImagePath);
					select last_insert_id();",
					("ClassName", @class.Name),
					("ImagePath", @class.ImagePath))
					.QuerySingleAsync<ulong>()));
			}

			var finalClassesList = new GetClassesRequest();
			finalClassesList.ClassIds.AddRange(insertIds);
			var createdClasses = await GetClasses(finalClassesList, context);

			reply.Classes.AddRange(createdClasses.Classes);
			return reply;
		}

		private Class MapClass(IDataRecord data, IReadOnlyList<ClassField> fields)
		{
			var @class = new Class();

			if (fields.Count == 0)
				fields = m_classDatabaseDefinition.BasicFields;

			if (fields.Contains(ClassField.ClassId))
				@class.Id = data.Get<int>(m_classDatabaseDefinition.GetSelectResult(ClassField.ClassId));
			if (fields.Contains(ClassField.ClassName))
				@class.Name = data.Get<string>(m_classDatabaseDefinition.GetSelectResult(ClassField.ClassName));
			if (fields.Contains(ClassField.ClassImagePath))
				@class.ImagePath = data.Get<string>(m_classDatabaseDefinition.GetSelectResult(ClassField.ClassImagePath));

			return @class;
		}

		public override async Task<CreateHenchmenReply> CreateHenchmen(CreateHenchmenRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new CreateHenchmenReply { Status = new Status { Code = 200 } };

			List<int> newHenchmenIds = new List<int>();

			foreach (var henchman in request.Henchmen)
			{
				// Validate the GamePackageId
				var packageRequest = new GetGamePackagesRequest();
				packageRequest.GamePackageIds.Add(henchman.GamePackageId);
				packageRequest.Fields.Add(GamePackageField.Id);
				var packageReply = await GetGamePackages(packageRequest, context);
				if (packageReply.Status.Code != 200)
				{
					reply.Status = packageReply.Status;
					return reply;
				}

				// Validate the AbilityIds
				var abilitiesRequest = new GetAbilitiesRequest();
				abilitiesRequest.AbilityIds.AddRange(henchman.AbilityIds);
				abilitiesRequest.AbilityFields.Add(AbilityField.Id);
				var abilitiesReply = await GetAbilities(abilitiesRequest, context);
				if (abilitiesReply.Status.Code != 200)
				{
					reply.Status = abilitiesReply.Status;
					return reply;
				}

				// Verify that this henchman doesn't already exist
				var henchmanRequest = new GetHenchmenRequest();
				henchmanRequest.Name  = henchman.Name;
				henchmanRequest.Fields.AddRange(new[] { HenchmanField.HenchmanId, HenchmanField.HenchmanName, HenchmanField.HenchmanGamePackageId });
				henchmanRequest.NameMatchStyle = NameMatchStyle.MixedCase;
				var henchmanReply = await GetHenchmen(henchmanRequest, context);
				if (henchmanReply.Status.Code == 200 && henchmanReply.Henchmen.Any())
				{
					var matchingHenchman = henchmanReply.Henchmen.First();
					reply.Status = new Status { Code = 400, Message = $"Henchman {matchingHenchman.Id} with name '{matchingHenchman.Name}' was found in game package '{matchingHenchman.GamePackageId}'" };
					return reply;
				}

				// Create the henchman
				var newHenchmanId = ((int)(await connector.Command($@"
					insert
						into {m_henchmanDatabaseDefinition.DefaultTableName}
							({m_henchmanDatabaseDefinition.ColumnName[HenchmanField.HenchmanName]})
									values (@HenchmanName);
								select last_insert_id();",
				("HenchmanName", henchman.Name))
				.QuerySingleAsync<ulong>()));

				// Add to game package
				await connector.Command(
					$@"
						insert
							into {TableNames.GamePackageHenchmen}
								({m_henchmanDatabaseDefinition.ColumnName[HenchmanField.HenchmanId]}, {m_gamePackageDatabaseDefinition.ColumnName[GamePackageField.Id]})
							values (@HenchmanId, @GamePackageId);",
					("HenchmanId", newHenchmanId),
					("GamePackageId", henchman.GamePackageId))
				.ExecuteAsync();

				// Link abilities
				foreach (var abilityId in henchman.AbilityIds)
				{
					await connector.Command(
						$@"
							insert
								into {TableNames.HenchmanAbilities}
									({m_henchmanDatabaseDefinition.ColumnName[HenchmanField.HenchmanId]}, {m_abilityDatabaseDefinition.ColumnName[AbilityField.Id]})
								values (@HenchmanId, @AbilityId);",
						("HenchmanId", newHenchmanId),
						("AbilityId", abilityId))
					.ExecuteAsync();
				}

				newHenchmenIds.Add(newHenchmanId);
			}

			// Get all of the created henchmen
			var finalRequest = new GetHenchmenRequest();
			finalRequest.HenchmanIds.AddRange(newHenchmenIds);
			var finalReply = await GetHenchmen(finalRequest, context);

			reply.Status = finalReply.Status;
			reply.Henchmen.AddRange(finalReply.Henchmen);

			return reply;
		}

		public override async Task<GetHenchmenReply> GetHenchmen(GetHenchmenRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new GetHenchmenReply { Status = new Status { Code = 200 } };

			if (request.Fields.Count() == 0)
				request.Fields.AddRange(m_henchmanDatabaseDefinition.BasicFields);

			// Need to remove abilityIds field because it's handled separately from the main db request
			var includeAbilityIds = request.Fields.Remove(HenchmanField.HenchmanAbilityIds);

			var select = m_henchmanDatabaseDefinition.BuildSelectStatement(request.Fields);
			var joins = m_henchmanDatabaseDefinition.BuildRequiredJoins(request.Fields);

			var where = !string.IsNullOrWhiteSpace(request.Name) ?
					$"where { m_henchmanDatabaseDefinition.BuildWhereStatement(HenchmanField.HenchmanName, GetWhereComparisonType(request.NameMatchStyle))}" :
					request.HenchmanIds.Count() != 0 ?
						$"where { m_henchmanDatabaseDefinition.BuildWhereStatement(HenchmanField.HenchmanId, WhereStatementType.Includes)}" :
						"";

			var whereMatch = !string.IsNullOrWhiteSpace(request.Name) ?
					new (string, object)[] { (m_henchmanDatabaseDefinition.GetSelectResult(HenchmanField.HenchmanName), request.NameMatchStyle == NameMatchStyle.Similar ? $"%{request.Name}%" : request.Name) } :
					request.HenchmanIds.Count() != 0 ?
						new (string, object)[] { (m_henchmanDatabaseDefinition.GetSelectResult(HenchmanField.HenchmanId), request.HenchmanIds.ToArray()) } :
						new (string, object)[] { };

			reply.Henchmen.AddRange(await db.RunCommand(connector,
				$@"select {select} from {m_henchmanDatabaseDefinition.DefaultTableName} {joins} {where};",
				whereMatch,
				x => MapHenchman(x, request.Fields)));

			if (includeAbilityIds)
			{
				// Lookup the abilities for each henchman
				foreach (var henchman in reply.Henchmen)
				{
					var abilitySelect = "AbilityId";
					henchman.AbilityIds.AddRange(await connector.Command($@"
						select {abilitySelect} from {TableNames.HenchmanAbilities} where HenchmanId = @HenchmanId;", ("HenchmanId", henchman.Id)).QueryAsync<int>());
				}
			}

			return reply;
		}

		private static WhereStatementType GetWhereComparisonType(NameMatchStyle matchStyle)
		{
			return matchStyle == NameMatchStyle.Similar ?
				WhereStatementType.Like :
				matchStyle == NameMatchStyle.MixedCase ? 
					WhereStatementType.Equals :
					WhereStatementType.BinaryEquals;
		}

		private Henchman MapHenchman(IDataRecord data, IReadOnlyList<HenchmanField> fields)
		{
			var henchman = new Henchman();

			if (fields.Count == 0)
				fields = m_henchmanDatabaseDefinition.BasicFields;

			if (fields.Contains(HenchmanField.HenchmanId))
				henchman.Id = data.Get<int>(m_henchmanDatabaseDefinition.GetSelectResult(HenchmanField.HenchmanId));
			if (fields.Contains(HenchmanField.HenchmanName))
				henchman.Name = data.Get<string>(m_henchmanDatabaseDefinition.GetSelectResult(HenchmanField.HenchmanName));
			if (fields.Contains(HenchmanField.HenchmanGamePackageId))
				henchman.GamePackageId = data.Get<int>(m_henchmanDatabaseDefinition.GetSelectResult(HenchmanField.HenchmanGamePackageId));

			return henchman;
		}


		public override async Task<CreateAdversariesReply> CreateAdversaries(CreateAdversariesRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new CreateAdversariesReply { Status = new Status { Code = 200 } };

			List<int> newAdversaryIds = new List<int>();

			foreach (var adversary in request.Adversaries)
			{
				// Validate the GamePackageId
				var packageRequest = new GetGamePackagesRequest();
				packageRequest.GamePackageIds.Add(adversary.GamePackageId);
				packageRequest.Fields.Add(GamePackageField.Id);
				var packageReply = await GetGamePackages(packageRequest, context);
				if (packageReply.Status.Code != 200)
				{
					reply.Status = packageReply.Status;
					return reply;
				}

				// Validate the AbilityIds
				var abilitiesRequest = new GetAbilitiesRequest();
				abilitiesRequest.AbilityIds.AddRange(adversary.AbilityIds);
				abilitiesRequest.AbilityFields.Add(AbilityField.Id);
				var abilitiesReply = await GetAbilities(abilitiesRequest, context);
				if (abilitiesReply.Status.Code != 200)
				{
					reply.Status = abilitiesReply.Status;
					return reply;
				}

				// Verify that this adversary doesn't already exist
				var adversaryRequest = new GetAdversariesRequest();
				adversaryRequest.Name = adversary.Name;
				adversaryRequest.Fields.AddRange(new[] { AdversaryField.AdversaryId, AdversaryField.AdversaryName, AdversaryField.AdversaryGamePackageId });
				adversaryRequest.NameMatchStyle = NameMatchStyle.MixedCase;
				var adversaryReply = await GetAdversaries(adversaryRequest, context);
				if (adversaryReply.Status.Code == 200 && adversaryReply.Adversaries.Any())
				{
					var matchingAdversary = adversaryReply.Adversaries.First();
					reply.Status = new Status { Code = 400, Message = $"Adversaries {matchingAdversary.Id} with name '{matchingAdversary.Name}' was found in game package '{matchingAdversary.GamePackageId}'" };
					return reply;
				}

				// Create the adversary
				var newAdversaryId = ((int)(await connector.Command($@"
					insert
						into {m_adversaryDatabaseDefinition.DefaultTableName}
							({m_adversaryDatabaseDefinition.ColumnName[AdversaryField.AdversaryName]})
									values (@AdversaryName);
								select last_insert_id();",
				("AdversaryName", adversary.Name))
				.QuerySingleAsync<ulong>()));

				// Add to game package
				await connector.Command(
					$@"
						insert
							into {TableNames.GamePackageAdversaries}
								({m_adversaryDatabaseDefinition.ColumnName[AdversaryField.AdversaryId]}, {m_gamePackageDatabaseDefinition.ColumnName[GamePackageField.Id]})
							values (@AdversaryId, @GamePackageId);",
					("AdversaryId", newAdversaryId),
					("GamePackageId", adversary.GamePackageId))
				.ExecuteAsync();

				// Link abilities
				foreach (var abilityId in adversary.AbilityIds)
				{
					await connector.Command(
						$@"
							insert
								into {TableNames.AdversaryAbilities}
									({m_adversaryDatabaseDefinition.ColumnName[AdversaryField.AdversaryId]}, {m_abilityDatabaseDefinition.ColumnName[AbilityField.Id]})
								values (@AdversaryId, @AbilityId);",
						("AdversaryId", newAdversaryId),
						("AbilityId", abilityId))
					.ExecuteAsync();
				}

				newAdversaryIds.Add(newAdversaryId);
			}

			// Get all of the created adversaries
			var finalRequest = new GetAdversariesRequest();
			finalRequest.AdversaryIds.AddRange(newAdversaryIds);
			var finalReply = await GetAdversaries(finalRequest, context);

			reply.Status = finalReply.Status;
			reply.Adversaries.AddRange(finalReply.Adversaries);

			return reply;
		}

		public override async Task<GetAdversariesReply> GetAdversaries(GetAdversariesRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new GetAdversariesReply { Status = new Status { Code = 200 } };

			if (request.Fields.Count() == 0)
				request.Fields.AddRange(m_adversaryDatabaseDefinition.BasicFields);

			// Need to remove abilityIds field because it's handled separately from the main db request
			var includeAbilityIds = request.Fields.Remove(AdversaryField.AdversaryAbilityIds);

			var select = m_adversaryDatabaseDefinition.BuildSelectStatement(request.Fields);
			var joins = m_adversaryDatabaseDefinition.BuildRequiredJoins(request.Fields);

			var where = !string.IsNullOrWhiteSpace(request.Name) ?
					$"where { m_adversaryDatabaseDefinition.BuildWhereStatement(AdversaryField.AdversaryName, GetWhereComparisonType(request.NameMatchStyle))}" :
					request.AdversaryIds.Count() != 0 ?
						$"where { m_adversaryDatabaseDefinition.BuildWhereStatement(AdversaryField.AdversaryId, WhereStatementType.Includes)}" :
						"";

			var whereMatch = !string.IsNullOrWhiteSpace(request.Name) ?
					new (string, object)[] { (m_adversaryDatabaseDefinition.GetSelectResult(AdversaryField.AdversaryName), request.NameMatchStyle == NameMatchStyle.Similar ? $"%{request.Name}%" : request.Name) } :
					request.AdversaryIds.Count() != 0 ?
						new (string, object)[] { (m_adversaryDatabaseDefinition.GetSelectResult(AdversaryField.AdversaryId), request.AdversaryIds.ToArray()) } :
						new (string, object)[] { };

			reply.Adversaries.AddRange(await db.RunCommand(connector,
				$@"select {select} from {m_adversaryDatabaseDefinition.DefaultTableName} {joins} {where};",
				whereMatch,
				x => MapAdversary(x, request.Fields)));

			if (includeAbilityIds)
			{
				// Lookup the abilities for each adversary
				foreach (var adversary in reply.Adversaries)
				{
					var abilitySelect = "AbilityId";
					adversary.AbilityIds.AddRange(await connector.Command($@"
						select {abilitySelect} from {TableNames.AdversaryAbilities} where AdversaryId = @AdversaryId;", ("AdversaryId", adversary.Id)).QueryAsync<int>());
				}
			}

			return reply;
		}

		private Adversary MapAdversary(IDataRecord data, IReadOnlyList<AdversaryField> fields)
		{
			var adversary = new Adversary();

			if (fields.Count == 0)
				fields = m_adversaryDatabaseDefinition.BasicFields;

			if (fields.Contains(AdversaryField.AdversaryId))
				adversary.Id = data.Get<int>(m_adversaryDatabaseDefinition.GetSelectResult(AdversaryField.AdversaryId));
			if (fields.Contains(AdversaryField.AdversaryName))
				adversary.Name = data.Get<string>(m_adversaryDatabaseDefinition.GetSelectResult(AdversaryField.AdversaryName));
			if (fields.Contains(AdversaryField.AdversaryGamePackageId))
				adversary.GamePackageId = data.Get<int>(m_adversaryDatabaseDefinition.GetSelectResult(AdversaryField.AdversaryGamePackageId));

			return adversary;
		}

		IDatabaseDefinition<AbilityField> m_abilityDatabaseDefinition;
		IDatabaseDefinition<GamePackageField> m_gamePackageDatabaseDefinition;
		IDatabaseDefinition<TeamField> m_teamDatabaseDefinition;
		IDatabaseDefinition<ClassField> m_classDatabaseDefinition;
		IDatabaseDefinition<HenchmanField> m_henchmanDatabaseDefinition;
		IDatabaseDefinition<AdversaryField> m_adversaryDatabaseDefinition;
	}
}
