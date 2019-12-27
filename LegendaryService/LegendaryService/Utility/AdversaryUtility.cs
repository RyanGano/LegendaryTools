using Faithlife.Data;
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
	public static class AdversaryUtility
	{
		public static async Task<CreateAdversariesReply> CreateAdversariesAsync(CreateAdversariesRequest request, ServerCallContext context)
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
				var packageReply = await GamePackageUtility.GetGamePackagesAsync(packageRequest, context);
				if (packageReply.Status.Code != 200)
				{
					reply.Status = packageReply.Status;
					return reply;
				}

				// Validate the AbilityIds
				var abilitiesRequest = new GetAbilitiesRequest();
				abilitiesRequest.AbilityIds.AddRange(adversary.AbilityIds);
				abilitiesRequest.AbilityFields.Add(AbilityField.Id);
				var abilitiesReply = await AbilityUtility.GetAbilitiesAsync(abilitiesRequest, context);
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
				var adversaryReply = await GetAdversariesAsync(adversaryRequest, context);
				if (adversaryReply.Status.Code == 200 && adversaryReply.Adversaries.Any())
				{
					var matchingAdversary = adversaryReply.Adversaries.First();
					reply.Status = new Status { Code = 400, Message = $"Adversaries {matchingAdversary.Id} with name '{matchingAdversary.Name}' was found in game package '{matchingAdversary.GamePackageId}'" };
					return reply;
				}

				// Create the adversary
				var newAdversaryId = ((int)(await connector.Command($@"
					insert
						into {DatabaseDefinition.DefaultTableName}
							({DatabaseDefinition.ColumnName[AdversaryField.AdversaryName]})
									values (@AdversaryName);
								select last_insert_id();",
				("AdversaryName", adversary.Name))
				.QuerySingleAsync<ulong>()));

				// Add to game package
				await connector.Command(
					$@"
						insert
							into {TableNames.GamePackageAdversaries}
								({DatabaseDefinition.ColumnName[AdversaryField.AdversaryId]}, {GamePackageUtility.DatabaseDefinition.ColumnName[GamePackageField.Id]})
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
									({DatabaseDefinition.ColumnName[AdversaryField.AdversaryId]}, {AbilityUtility.DatabaseDefinition.ColumnName[AbilityField.Id]})
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
			var finalReply = await GetAdversariesAsync(finalRequest, context);

			reply.Status = finalReply.Status;
			reply.Adversaries.AddRange(finalReply.Adversaries);

			return reply;
		}

		public static async Task<GetAdversariesReply> GetAdversariesAsync(GetAdversariesRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new GetAdversariesReply { Status = new Status { Code = 200 } };

			if (request.Fields.Count() == 0)
				request.Fields.AddRange(DatabaseDefinition.BasicFields);

			// Need to remove abilityIds field because it's handled separately from the main db request
			var includeAbilityIds = request.Fields.Remove(AdversaryField.AdversaryAbilityIds);

			var select = DatabaseDefinition.BuildSelectStatement(request.Fields);
			var joins = DatabaseDefinition.BuildRequiredJoins(request.Fields);

			var where = !string.IsNullOrWhiteSpace(request.Name) ?
					$"where { DatabaseDefinition.BuildWhereStatement(AdversaryField.AdversaryName, DatabaseUtility.GetWhereComparisonType(request.NameMatchStyle))}" :
					request.AdversaryIds.Count() != 0 ?
						$"where { DatabaseDefinition.BuildWhereStatement(AdversaryField.AdversaryId, WhereStatementType.Includes)}" :
						"";

			var whereMatch = !string.IsNullOrWhiteSpace(request.Name) ?
					new (string, object)[] { (DatabaseDefinition.GetSelectResult(AdversaryField.AdversaryName), request.NameMatchStyle == NameMatchStyle.Similar ? $"%{request.Name}%" : request.Name) } :
					request.AdversaryIds.Count() != 0 ?
						new (string, object)[] { (DatabaseDefinition.GetSelectResult(AdversaryField.AdversaryId), request.AdversaryIds.ToArray()) } :
						new (string, object)[] { };

			reply.Adversaries.AddRange(await db.RunCommand(connector,
				$@"select {select} from {DatabaseDefinition.DefaultTableName} {joins} {where};",
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

		private static Adversary MapAdversary(IDataRecord data, IReadOnlyList<AdversaryField> fields)
		{
			var adversary = new Adversary();

			if (fields.Count == 0)
				fields = DatabaseDefinition.BasicFields;

			if (fields.Contains(AdversaryField.AdversaryId))
				adversary.Id = data.Get<int>(DatabaseDefinition.GetSelectResult(AdversaryField.AdversaryId));
			if (fields.Contains(AdversaryField.AdversaryName))
				adversary.Name = data.Get<string>(DatabaseDefinition.GetSelectResult(AdversaryField.AdversaryName));
			if (fields.Contains(AdversaryField.AdversaryGamePackageId))
				adversary.GamePackageId = data.Get<int>(DatabaseDefinition.GetSelectResult(AdversaryField.AdversaryGamePackageId));

			return adversary;
		}

		public static IDatabaseDefinition<AdversaryField> DatabaseDefinition = new AdversaryDatabaseDefinition();
	}
}
