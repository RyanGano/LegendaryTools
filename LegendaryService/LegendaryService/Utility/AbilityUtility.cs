using Faithlife.Data;
using Faithlife.Utility;
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
	public static class AbilityUtility
	{
		public static async Task<GetAbilitiesReply> GetAbilitiesAsync(GetAbilitiesRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });
			var reply = new GetAbilitiesReply { Status = new Status { Code = 200 } };

			var select = DatabaseDefinition.BuildSelectStatement(request.AbilityFields);
			var joins = DatabaseDefinition.BuildRequiredJoins(request.AbilityFields);
			var where = request.GamePackageId != 0 ?
				$"where { DatabaseDefinition.BuildWhereStatement(AbilityField.GamePackageId, WhereStatementType.Equals)}" :
				request.AbilityIds.Count() != 0 ?
					$"where { DatabaseDefinition.BuildWhereStatement(AbilityField.Id, WhereStatementType.Includes)}" :
					!string.IsNullOrWhiteSpace(request.Name) ?
						$"where { DatabaseDefinition.BuildWhereStatement(AbilityField.Name, WhereStatementType.Like)}" :
						"";

			var whereMatch = request.GamePackageId != 0 ?
				new (string, object)[] { (DatabaseDefinition.GetSelectResult(AbilityField.GamePackageId), request.GamePackageId) } :
				request.AbilityIds.Count() != 0 ?
					new (string, object)[] { (DatabaseDefinition.GetSelectResult(AbilityField.Id), request.AbilityIds.ToArray()) } :
					!string.IsNullOrWhiteSpace(request.Name) ?
						new (string, object)[] { (DatabaseDefinition.GetSelectResult(AbilityField.Name), $"%{request.Name}%") } :
						new (string, object)[] { };

			reply.Abilities.AddRange(await db.RunCommand(connector,
				$@"select {select} from {DatabaseDefinition.DefaultTableName} {joins} {where};",
				whereMatch,
				x => MapAbility(x, request.AbilityFields, null)));

			return reply;
		}

		public static async Task<CreateAbilitiesReply> CreateAbilitiesAsync(CreateAbilitiesRequest request, ServerCallContext context)
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

			var existingAbilities = await GetExistingAbilitiesAsync(request.Abilities, connector);
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

			existingAbilities = await GetExistingAbilitiesAsync(request.Abilities, connector);

			reply.Abilities.AddRange(existingAbilities);
			return reply;
		}

		public static async ValueTask<IReadOnlyList<Ability>> GetAllAbilitiesAsync(IReadOnlyList<GamePackage> gamePackages, DbConnector connector)
		{
			var test = (await connector.Command($@"select {DatabaseDefinition.BuildSelectStatement(null)} from abilities;").QueryAsync(x => MapAbility(x, DatabaseDefinition.BasicFields, gamePackages))).ToList();
			return test;
		}

		public static async ValueTask<IReadOnlyList<Ability>> GetExistingAbilitiesAsync(RepeatedField<Ability> abilities, DbConnector connector)
		{
			var foundAbilities = new List<Ability>();

			var gamePackages = abilities.Select(x => x.GamePackage).DistinctBy(x => x.Id).ToList();

			foreach (var gamePackageAbilities in abilities.GroupBy(x => x.GamePackage.Id))
			{
				foundAbilities.AddRange(await connector.Command($@"select {DatabaseDefinition.BuildSelectStatement(null)} from abilities where name REGEXP @Names and GamePackageId = @GamePackageId;",
					("Names", gamePackageAbilities.Select(x => LegendaryDatabase.Encode(x.Name)).Join("|")),
					("GamePackageId", gamePackageAbilities.Key))
					.QueryAsync(x => MapAbility(x, DatabaseDefinition.BasicFields, gamePackages)));
			}

			return foundAbilities;
		}

		private static Ability MapAbility(IDataRecord data, IReadOnlyList<AbilityField> fields, IReadOnlyList<GamePackage> gamePackages)
		{
			var gamePackage = gamePackages?.FirstOrDefault(x => x.Id == data.Get<int>(DatabaseDefinition.GetSelectResult(AbilityField.GamePackageId)));

			if (gamePackage == null && (fields.Contains(AbilityField.GamePackageId) || fields.Contains(AbilityField.GamePackageName)))
			{
				gamePackage = new GamePackage();

				if (fields.Contains(AbilityField.GamePackageId))
					gamePackage.Id = data.Get<int>(DatabaseDefinition.GetSelectResult(AbilityField.GamePackageId));

				if (fields.Contains(AbilityField.GamePackageName))
					gamePackage.Name = data.Get<string>(DatabaseDefinition.GetSelectResult(AbilityField.GamePackageName));
			}

			return new Ability
			{
				Id = fields.Contains(AbilityField.Id) ? data.Get<int>(DatabaseDefinition.GetSelectResult(AbilityField.Id)) : 0,
				Name = fields.Contains(AbilityField.Name) ? data.Get<string>(DatabaseDefinition.GetSelectResult(AbilityField.Name)) : "",
				Description = fields.Contains(AbilityField.Description) ? data.Get<string>(DatabaseDefinition.GetSelectResult(AbilityField.Description)) : "",
				GamePackage = gamePackage
			};
		}

		public static IDatabaseDefinition<AbilityField> DatabaseDefinition = new AbilityDatabaseDefinition();
	}
}
