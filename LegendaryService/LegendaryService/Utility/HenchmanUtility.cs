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
	public static class HenchmanUtility
	{
		public static async Task<CreateHenchmenReply> CreateHenchmenAsync(CreateHenchmenRequest request, ServerCallContext context)
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
				var packageReply = await GamePackageUtility.GetGamePackagesAsync(packageRequest, context);
				if (packageReply.Status.Code != 200)
				{
					reply.Status = packageReply.Status;
					return reply;
				}

				// Validate the AbilityIds
				var abilitiesRequest = new GetAbilitiesRequest();
				abilitiesRequest.AbilityIds.AddRange(henchman.AbilityIds);
				abilitiesRequest.AbilityFields.Add(AbilityField.Id);
				var abilitiesReply = await AbilityUtility.GetAbilitiesAsync(abilitiesRequest, context);
				if (abilitiesReply.Status.Code != 200)
				{
					reply.Status = abilitiesReply.Status;
					return reply;
				}

				// Verify that this henchman doesn't already exist
				var henchmanRequest = new GetHenchmenRequest();
				henchmanRequest.Name = henchman.Name;
				henchmanRequest.Fields.AddRange(new[] { HenchmanField.HenchmanId, HenchmanField.HenchmanName, HenchmanField.HenchmanGamePackageId });
				henchmanRequest.NameMatchStyle = NameMatchStyle.MixedCase;
				var henchmanReply = await GetHenchmenAsync(henchmanRequest, context);
				if (henchmanReply.Status.Code == 200 && henchmanReply.Henchmen.Any())
				{
					var matchingHenchman = henchmanReply.Henchmen.First();
					reply.Status = new Status { Code = 400, Message = $"Henchman {matchingHenchman.Id} with name '{matchingHenchman.Name}' was found in game package '{matchingHenchman.GamePackageId}'" };
					return reply;
				}

				// Create the henchman
				var newHenchmanId = ((int)(await connector.Command($@"
					insert
						into {DatabaseDefinition.DefaultTableName}
							({DatabaseDefinition.ColumnName[HenchmanField.HenchmanName]})
									values (@HenchmanName);
								select last_insert_id();",
				("HenchmanName", henchman.Name))
				.QuerySingleAsync<ulong>()));

				// Add to game package
				await connector.Command(
					$@"
						insert
							into {TableNames.GamePackageHenchmen}
								({DatabaseDefinition.ColumnName[HenchmanField.HenchmanId]}, {GamePackageUtility.DatabaseDefinition.ColumnName[GamePackageField.Id]})
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
									({DatabaseDefinition.ColumnName[HenchmanField.HenchmanId]}, {AbilityUtility.DatabaseDefinition.ColumnName[AbilityField.Id]})
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
			var finalReply = await GetHenchmenAsync(finalRequest, context);

			reply.Status = finalReply.Status;
			reply.Henchmen.AddRange(finalReply.Henchmen);

			return reply;
		}

		public static async Task<GetHenchmenReply> GetHenchmenAsync(GetHenchmenRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new GetHenchmenReply { Status = new Status { Code = 200 } };

			if (request.Fields.Count() == 0)
				request.Fields.AddRange(DatabaseDefinition.BasicFields);

			// Need to remove abilityIds field because it's handled separately from the main db request
			var includeAbilityIds = request.Fields.Remove(HenchmanField.HenchmanAbilityIds);

			var select = DatabaseDefinition.BuildSelectStatement(request.Fields);
			var joins = DatabaseDefinition.BuildRequiredJoins(request.Fields);

			var where = !string.IsNullOrWhiteSpace(request.Name) ?
					$"where { DatabaseDefinition.BuildWhereStatement(HenchmanField.HenchmanName, DatabaseUtility.GetWhereComparisonType(request.NameMatchStyle))}" :
					request.HenchmanIds.Count() != 0 ?
						$"where { DatabaseDefinition.BuildWhereStatement(HenchmanField.HenchmanId, WhereStatementType.Includes)}" :
						"";

			var whereMatch = !string.IsNullOrWhiteSpace(request.Name) ?
					new (string, object)[] { (DatabaseDefinition.GetSelectResult(HenchmanField.HenchmanName), request.NameMatchStyle == NameMatchStyle.Similar ? $"%{request.Name}%" : request.Name) } :
					request.HenchmanIds.Count() != 0 ?
						new (string, object)[] { (DatabaseDefinition.GetSelectResult(HenchmanField.HenchmanId), request.HenchmanIds.ToArray()) } :
						new (string, object)[] { };

			reply.Henchmen.AddRange(await db.RunCommand(connector,
				$@"select {select} from {DatabaseDefinition.DefaultTableName} {joins} {where};",
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

		private static Henchman MapHenchman(IDataRecord data, IReadOnlyList<HenchmanField> fields)
		{
			var henchman = new Henchman();

			if (fields.Count == 0)
				fields = DatabaseDefinition.BasicFields;

			if (fields.Contains(HenchmanField.HenchmanId))
				henchman.Id = data.Get<int>(DatabaseDefinition.GetSelectResult(HenchmanField.HenchmanId));
			if (fields.Contains(HenchmanField.HenchmanName))
				henchman.Name = data.Get<string>(DatabaseDefinition.GetSelectResult(HenchmanField.HenchmanName));
			if (fields.Contains(HenchmanField.HenchmanGamePackageId))
				henchman.GamePackageId = data.Get<int>(DatabaseDefinition.GetSelectResult(HenchmanField.HenchmanGamePackageId));

			return henchman;
		}
		
		public static IDatabaseDefinition<HenchmanField> DatabaseDefinition = new HenchmanDatabaseDefinition();
	}
}
