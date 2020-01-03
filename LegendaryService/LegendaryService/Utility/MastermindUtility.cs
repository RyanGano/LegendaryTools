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
	public static class MastermindUtility
	{
		public static async Task<CreateMastermindsReply> CreateMastermindsAsync(CreateMastermindsRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new CreateMastermindsReply { Status = new Status { Code = 200 } };

			List<int> newMastermindIds = new List<int>();

			var cardSetTypeInfo = await CardRequirementUtility.GetInsertCardTypeValue(CardSetType.CardSetMastermind, connector);

			foreach (var mastermind in request.Masterminds)
			{
				// Validate the GamePackageId
				var packageRequest = new GetGamePackagesRequest();
				packageRequest.GamePackageIds.Add(mastermind.GamePackageId);
				packageRequest.Fields.Add(GamePackageField.Id);
				var packageReply = await GamePackageUtility.GetGamePackagesAsync(packageRequest, context);
				if (packageReply.Status.Code != 200)
				{
					reply.Status = packageReply.Status;
					return reply;
				}

				// Validate the AbilityIds
				var abilitiesRequest = new GetAbilitiesRequest();
				abilitiesRequest.AbilityIds.AddRange(mastermind.AbilityIds);
				abilitiesRequest.AbilityFields.Add(AbilityField.Id);
				var abilitiesReply = await AbilityUtility.GetAbilitiesAsync(abilitiesRequest, context);
				if (abilitiesReply.Status.Code != 200)
				{
					reply.Status = abilitiesReply.Status;
					return reply;
				}

				// Verify that this mastermind doesn't already exist
				var mastermindRequest = new GetMastermindsRequest();
				mastermindRequest.Name = mastermind.Name;
				mastermindRequest.Fields.AddRange(new[] { MastermindField.MastermindId, MastermindField.MastermindName, MastermindField.MastermindGamePackageId });
				mastermindRequest.NameMatchStyle = NameMatchStyle.MixedCase;
				var mastermindReply = await GetMastermindsAsync(mastermindRequest, context);
				if (mastermindReply.Status.Code == 200 && mastermindReply.Masterminds.Any())
				{
					var matchingMastermind = mastermindReply.Masterminds.First();
					reply.Status = new Status { Code = 400, Message = $"Mastermind {matchingMastermind.Id} with name '{matchingMastermind.Name}' was found in game package '{matchingMastermind.GamePackageId}'" };
					return reply;
				}

				// Create the mastermind
				var newMastermindId = ((int)(await connector.Command($@"
					insert
						into {DatabaseDefinition.DefaultTableName}
							({DatabaseDefinition.ColumnName[MastermindField.MastermindName]}, {DatabaseDefinition.ColumnName[MastermindField.MastermindEpicInformation]})
									values (@MastermindName, @HasEpicSide);
								select last_insert_id();",
				("MastermindName", mastermind.Name),
				("HasEpicSide", mastermind.HasEpicSide))
				.QuerySingleAsync<ulong>()));

				// Add to game package
				await connector.Command(
					$@"
						insert
							into {TableNames.GamePackageMasterminds}
								({DatabaseDefinition.ColumnName[MastermindField.MastermindId]}, {GamePackageUtility.DatabaseDefinition.ColumnName[GamePackageField.Id]})
							values (@MastermindId, @GamePackageId);",
					("MastermindId", newMastermindId),
					("GamePackageId", mastermind.GamePackageId))
				.ExecuteAsync();

				// Link abilities
				foreach (var abilityId in mastermind.AbilityIds)
				{
					await connector.Command(
						$@"
							insert
								into {TableNames.MastermindAbilities}
									({DatabaseDefinition.ColumnName[MastermindField.MastermindId]}, {AbilityUtility.DatabaseDefinition.ColumnName[AbilityField.Id]})
								values (@MastermindId, @AbilityId);",
						("MastermindId", newMastermindId),
						("AbilityId", abilityId))
					.ExecuteAsync();
				}

				// Add card requirements
				foreach (var requirement in mastermind.CardRequirements)
				{
					var cardRequirementId = await CardRequirementUtility.AddCardRequirement(requirement);

					await connector.Command(
						$@"
						insert
							into {TableNames.MatchedCardRequirements}
								({CardRequirementUtility.DatabaseDefinition.ColumnName[CardRequirement.OwnerIdFieldNumber]}, {CardRequirementUtility.DatabaseDefinition.ColumnName[CardRequirement.CardRequirementIdFieldNumber]}, NumberOfPlayers, {cardSetTypeInfo.Name})
							values (@OwnerId, @CardRequirementId, @NumberOfPlayers, @{cardSetTypeInfo.Name});",
						("OwnerId", newMastermindId),
						("CardRequirementId", cardRequirementId),
						("NumberOfPlayers", requirement.PlayerCount),
						(cardSetTypeInfo.Name, cardSetTypeInfo.Value))
					.ExecuteAsync();
				}

			newMastermindIds.Add(newMastermindId);
			}

			// Get all of the created masterminds
			var finalRequest = new GetMastermindsRequest();
			finalRequest.MastermindIds.AddRange(newMastermindIds);
			var finalReply = await GetMastermindsAsync(finalRequest, context);

			reply.Status = finalReply.Status;
			reply.Masterminds.AddRange(finalReply.Masterminds);

			return reply;
		}

		public static async Task<GetMastermindsReply> GetMastermindsAsync(GetMastermindsRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new GetMastermindsReply { Status = new Status { Code = 200 } };

			if (request.Fields.Count() == 0)
				request.Fields.AddRange(DatabaseDefinition.BasicFields);

			// Need to remove abilityIds field because it's handled separately from the main db request
			var includeAbilityIds = request.Fields.Remove(MastermindField.MastermindAbilityIds);
			var includeCardRequirements = request.Fields.Remove(MastermindField.MastermindCardRequirements);

			var select = DatabaseDefinition.BuildSelectStatement(request.Fields);
			var joins = DatabaseDefinition.BuildRequiredJoins(request.Fields);

			var where = !string.IsNullOrWhiteSpace(request.Name) ?
					$"where { DatabaseDefinition.BuildWhereStatement(MastermindField.MastermindName, DatabaseUtility.GetWhereComparisonType(request.NameMatchStyle))}" :
					request.MastermindIds.Count() != 0 ?
						$"where { DatabaseDefinition.BuildWhereStatement(MastermindField.MastermindId, WhereStatementType.Includes)}" :
						"";

			var whereMatch = !string.IsNullOrWhiteSpace(request.Name) ?
					new (string, object)[] { (DatabaseDefinition.GetSelectResult(MastermindField.MastermindName), request.NameMatchStyle == NameMatchStyle.Similar ? $"%{request.Name}%" : request.Name) } :
					request.MastermindIds.Count() != 0 ?
						new (string, object)[] { (DatabaseDefinition.GetSelectResult(MastermindField.MastermindId), request.MastermindIds.ToArray()) } :
						new (string, object)[] { };

			reply.Masterminds.AddRange(await db.RunCommand(connector,
				$@"select {select} from {DatabaseDefinition.DefaultTableName} {joins} {where};",
				whereMatch,
				x => MapMastermind(x, request.Fields)));

			if (includeAbilityIds)
			{
				// Lookup the abilities for each mastermind
				foreach (var mastermind in reply.Masterminds)
				{
					var abilitySelect = "AbilityId";
					mastermind.AbilityIds.AddRange(await connector.Command($@"
						select {abilitySelect} from {TableNames.MastermindAbilities} where MastermindId = @MastermindId;", ("MastermindId", mastermind.Id)).QueryAsync<int>());
				}
			}

			if (includeCardRequirements)
			{
				// Lookup the card requirements for each mastermind
				foreach (var mastermind in reply.Masterminds)
					mastermind.CardRequirements.AddRange(await CardRequirementUtility.GetCardRequirementsAsync(mastermind.Id));
			}

			return reply;
		}

		private static Mastermind MapMastermind(IDataRecord data, IReadOnlyList<MastermindField> fields)
		{
			var mastermind = new Mastermind();

			if (fields.Count == 0)
				fields = DatabaseDefinition.BasicFields;

			if (fields.Contains(MastermindField.MastermindId))
				mastermind.Id = data.Get<int>(DatabaseDefinition.GetSelectResult(MastermindField.MastermindId));
			if (fields.Contains(MastermindField.MastermindName))
				mastermind.Name = data.Get<string>(DatabaseDefinition.GetSelectResult(MastermindField.MastermindName));
			if (fields.Contains(MastermindField.MastermindEpicInformation))
				mastermind.HasEpicSide = data.Get<bool>(DatabaseDefinition.GetSelectResult(MastermindField.MastermindEpicInformation));
			if (fields.Contains(MastermindField.MastermindGamePackageId))
				mastermind.GamePackageId = data.Get<int>(DatabaseDefinition.GetSelectResult(MastermindField.MastermindGamePackageId));

			return mastermind;
		}
		
		public static IDatabaseDefinition<MastermindField> DatabaseDefinition = new MastermindDatabaseDefinition();
	}
}
