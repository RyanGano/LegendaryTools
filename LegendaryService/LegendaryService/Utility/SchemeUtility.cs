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
	public static class SchemeUtility
	{
		public static async Task<CreateSchemesReply> CreateSchemesAsync(CreateSchemesRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new CreateSchemesReply { Status = new Status { Code = 200 } };

			List<int> newSchemeIds = new List<int>();

			var cardSetTypeInfo = await CardRequirementUtility.GetInsertCardTypeValue(CardSetType.CardSetScheme, connector);

			foreach (var scheme in request.Schemes)
			{
				// Validate the GamePackageId
				var packageRequest = new GetGamePackagesRequest();
				packageRequest.GamePackageIds.Add(scheme.GamePackageId);
				packageRequest.Fields.Add(GamePackageField.Id);
				var packageReply = await GamePackageUtility.GetGamePackagesAsync(packageRequest, context);
				if (packageReply.Status.Code != 200)
				{
					reply.Status = packageReply.Status;
					return reply;
				}

				// Validate the AbilityIds
				var abilitiesRequest = new GetAbilitiesRequest();
				abilitiesRequest.AbilityIds.AddRange(scheme.AbilityIds);
				abilitiesRequest.AbilityFields.Add(AbilityField.Id);
				var abilitiesReply = await AbilityUtility.GetAbilitiesAsync(abilitiesRequest, context);
				if (abilitiesReply.Status.Code != 200)
				{
					reply.Status = abilitiesReply.Status;
					return reply;
				}

				// Verify that this scheme doesn't already exist
				var schemeRequest = new GetSchemesRequest();
				schemeRequest.Name = scheme.Name;
				schemeRequest.Fields.AddRange(new[] { SchemeField.SchemeId, SchemeField.SchemeName, SchemeField.SchemeGamePackageId });
				schemeRequest.NameMatchStyle = NameMatchStyle.MixedCase;
				var schemeReply = await GetSchemesAsync(schemeRequest, context);
				if (schemeReply.Status.Code == 200 && schemeReply.Schemes.Any())
				{
					var matchingScheme = schemeReply.Schemes.First();
					reply.Status = new Status { Code = 400, Message = $"Scheme {matchingScheme.Id} with name '{matchingScheme.Name}' was found in game package '{matchingScheme.GamePackageId}'" };
					return reply;
				}

				// Create the scheme
				var newSchemeId = ((int)(await connector.Command($@"
					insert
						into {DatabaseDefinition.DefaultTableName}
							({DatabaseDefinition.ColumnName[SchemeField.SchemeName]}, {DatabaseDefinition.ColumnName[SchemeField.SchemeEpicInformation]})
									values (@SchemeName, @HasEpicSide);
								select last_insert_id();",
				("SchemeName", scheme.Name),
				("HasEpicSide", scheme.HasEpicSide))
				.QuerySingleAsync<ulong>()));

				// Add to game package
				await connector.Command(
					$@"
						insert
							into {TableNames.GamePackageSchemes}
								({DatabaseDefinition.ColumnName[SchemeField.SchemeId]}, {GamePackageUtility.DatabaseDefinition.ColumnName[GamePackageField.Id]})
							values (@SchemeId, @GamePackageId);",
					("SchemeId", newSchemeId),
					("GamePackageId", scheme.GamePackageId))
				.ExecuteAsync();

				// Link abilities
				foreach (var abilityId in scheme.AbilityIds)
				{
					await connector.Command(
						$@"
							insert
								into {TableNames.SchemeAbilities}
									({DatabaseDefinition.ColumnName[SchemeField.SchemeId]}, {AbilityUtility.DatabaseDefinition.ColumnName[AbilityField.Id]})
								values (@SchemeId, @AbilityId);",
						("SchemeId", newSchemeId),
						("AbilityId", abilityId))
					.ExecuteAsync();
				}

				// Add card requirements
				foreach (var requirement in scheme.CardRequirements)
				{
					var cardRequirementId = await CardRequirementUtility.AddCardRequirement(requirement);

					await connector.Command(
						$@"
						insert
							into {TableNames.MatchedCardRequirements}
								({CardRequirementUtility.DatabaseDefinition.ColumnName[CardRequirement.OwnerIdFieldNumber]}, {CardRequirementUtility.DatabaseDefinition.ColumnName[CardRequirement.CardRequirementIdFieldNumber]}, NumberOfPlayers, {cardSetTypeInfo.Name})
							values (@OwnerId, @CardRequirementId, @NumberOfPlayers, @{cardSetTypeInfo.Name});",
						("OwnerId", newSchemeId),
						("CardRequirementId", cardRequirementId),
						("NumberOfPlayers", requirement.PlayerCount),
						(cardSetTypeInfo.Name, cardSetTypeInfo.Value))
					.ExecuteAsync();
				}

				// Add twist requirements
				foreach (var twistRequirement in scheme.TwistRequirements)
				{
					var twistRequirementId = await SchemeTwistRequirementUtility.AddSchemeTwistRequirementAsync(twistRequirement);

					await connector.Command(
						$@"
						insert
							into {TableNames.SchemeTwistRequirements}
								(SchemeId, TwistRequirementId, NumberOfPlayers)
							values (@SchemeId, @TwistRequirementId, @NumberOfPlayers);",
						("SchemeId", newSchemeId),
						("TwistRequirementId", twistRequirementId),
						("NumberOfPlayers", twistRequirement.PlayerCount))
					.ExecuteAsync();
				}

				newSchemeIds.Add(newSchemeId);
			}

			// Get all of the created schemes
			var finalRequest = new GetSchemesRequest();
			finalRequest.SchemeIds.AddRange(newSchemeIds);
			var finalReply = await GetSchemesAsync(finalRequest, context);

			reply.Status = finalReply.Status;
			reply.Schemes.AddRange(finalReply.Schemes);

			return reply;
		}

		public static async Task<GetSchemesReply> GetSchemesAsync(GetSchemesRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new GetSchemesReply { Status = new Status { Code = 200 } };

			if (request.Fields.Count() == 0)
				request.Fields.AddRange(DatabaseDefinition.BasicFields);

			// Need to remove abilityIds field because it's handled separately from the main db request
			var includeAbilityIds = request.Fields.Remove(SchemeField.SchemeAbilityIds);
			var includeCardRequirements = request.Fields.Remove(SchemeField.SchemeCardRequirements);

			var select = DatabaseDefinition.BuildSelectStatement(request.Fields);
			var joins = DatabaseDefinition.BuildRequiredJoins(request.Fields);

			var where = !string.IsNullOrWhiteSpace(request.Name) ?
					$"where { DatabaseDefinition.BuildWhereStatement(SchemeField.SchemeName, DatabaseUtility.GetWhereComparisonType(request.NameMatchStyle))}" :
					request.SchemeIds.Count() != 0 ?
						$"where { DatabaseDefinition.BuildWhereStatement(SchemeField.SchemeId, WhereStatementType.Includes)}" :
						"";

			var whereMatch = !string.IsNullOrWhiteSpace(request.Name) ?
					new (string, object)[] { (DatabaseDefinition.GetSelectResult(SchemeField.SchemeName), request.NameMatchStyle == NameMatchStyle.Similar ? $"%{request.Name}%" : request.Name) } :
					request.SchemeIds.Count() != 0 ?
						new (string, object)[] { (DatabaseDefinition.GetSelectResult(SchemeField.SchemeId), request.SchemeIds.ToArray()) } :
						new (string, object)[] { };

			reply.Schemes.AddRange(await db.RunCommand(connector,
				$@"select {select} from {DatabaseDefinition.DefaultTableName} {joins} {where};",
				whereMatch,
				x => MapScheme(x, request.Fields)));

			if (includeAbilityIds)
			{
				// Lookup the abilities for each scheme
				foreach (var scheme in reply.Schemes)
				{
					var abilitySelect = "AbilityId";
					scheme.AbilityIds.AddRange(await connector.Command($@"
						select {abilitySelect} from {TableNames.SchemeAbilities} where SchemeId = @SchemeId;", ("SchemeId", scheme.Id)).QueryAsync<int>());
				}
			}

			if (includeCardRequirements)
			{
				// Lookup the card requirements for each scheme
				foreach (var scheme in reply.Schemes)
					scheme.CardRequirements.AddRange(await CardRequirementUtility.GetCardRequirementsAsync(scheme.Id));
			}

			foreach (var scheme in reply.Schemes)
				scheme.TwistRequirements.AddRange(await SchemeTwistRequirementUtility.GetSchemeTwistRequirementsAsync(scheme.Id));

			return reply;
		}

		private static Scheme MapScheme(IDataRecord data, IReadOnlyList<SchemeField> fields)
		{
			var scheme = new Scheme();

			if (fields.Count == 0)
				fields = DatabaseDefinition.BasicFields;

			if (fields.Contains(SchemeField.SchemeId))
				scheme.Id = data.Get<int>(DatabaseDefinition.GetSelectResult(SchemeField.SchemeId));
			if (fields.Contains(SchemeField.SchemeName))
				scheme.Name = data.Get<string>(DatabaseDefinition.GetSelectResult(SchemeField.SchemeName));
			if (fields.Contains(SchemeField.SchemeEpicInformation))
				scheme.HasEpicSide = data.Get<bool>(DatabaseDefinition.GetSelectResult(SchemeField.SchemeEpicInformation));
			if (fields.Contains(SchemeField.SchemeGamePackageId))
				scheme.GamePackageId = data.Get<int>(DatabaseDefinition.GetSelectResult(SchemeField.SchemeGamePackageId));

			return scheme;
		}
		
		public static IDatabaseDefinition<SchemeField> DatabaseDefinition = new SchemeDatabaseDefinition();
	}
}
