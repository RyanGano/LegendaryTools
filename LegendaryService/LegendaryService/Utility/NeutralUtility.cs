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
	public class NeutralUtility
	{
		public static async Task<CreateNeutralsReply> CreateNeutralsAsync(CreateNeutralsRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new CreateNeutralsReply { Status = new Status { Code = 200 } };

			List<int> newNeutralIds = new List<int>();

			foreach (var neutral in request.Neutrals)
			{
				// Validate the GamePackageId
				var packageRequest = new GetGamePackagesRequest();
				packageRequest.GamePackageIds.Add(neutral.GamePackageId);
				packageRequest.Fields.Add(GamePackageField.Id);
				var packageReply = await GamePackageUtility.GetGamePackagesAsync(packageRequest, context);
				if (packageReply.Status.Code != 200)
				{
					reply.Status = packageReply.Status;
					return reply;
				}

				// Verify that this neutral doesn't already exist
				var neutralRequest = new GetNeutralsRequest();
				neutralRequest.Name = neutral.Name;
				neutralRequest.Fields.AddRange(new[] { NeutralField.NeutralId, NeutralField.NeutralName, NeutralField.NeutralGamePackageId });
				neutralRequest.NameMatchStyle = NameMatchStyle.MixedCase;
				var neutralReply = await GetNeutralsAsync(neutralRequest, context);
				if (neutralReply.Status.Code == 200 && neutralReply.Neutrals.Any(x => x.GamePackageId == neutral.GamePackageId))
				{
					var matchingNeutral = neutralReply.Neutrals.First();
					reply.Status = new Status { Code = 400, Message = $"Neutral {matchingNeutral.Id} with name '{matchingNeutral.Name}' was found in game package '{matchingNeutral.GamePackageId}'" };
					return reply;
				}

				// Create the neutral
				var newNeutralId = ((int)(await connector.Command($@"
					insert
						into {DatabaseDefinition.DefaultTableName}
							({DatabaseDefinition.ColumnName[NeutralField.NeutralName]})
									values (@NeutralName);
								select last_insert_id();",
				("NeutralName", neutral.Name))
				.QuerySingleAsync<ulong>()));

				// Add to game package
				await connector.Command(
					$@"
						insert
							into {TableNames.GamePackageNeutrals}
								({DatabaseDefinition.ColumnName[NeutralField.NeutralId]}, {GamePackageUtility.DatabaseDefinition.ColumnName[GamePackageField.Id]})
							values (@NeutralId, @GamePackageId);",
					("NeutralId", newNeutralId),
					("GamePackageId", neutral.GamePackageId))
				.ExecuteAsync();

				newNeutralIds.Add(newNeutralId);
			}

			// Get all of the created neutrals
			var finalRequest = new GetNeutralsRequest();
			finalRequest.NeutralIds.AddRange(newNeutralIds);
			var finalReply = await GetNeutralsAsync(finalRequest, context);

			reply.Status = finalReply.Status;
			reply.Neutrals.AddRange(finalReply.Neutrals);

			return reply;
		}

		public static async Task<GetNeutralsReply> GetNeutralsAsync(GetNeutralsRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new GetNeutralsReply { Status = new Status { Code = 200 } };

			var select = DatabaseDefinition.BuildSelectStatement(request.Fields);
			var joins = DatabaseDefinition.BuildRequiredJoins(request.Fields);

			var where = !string.IsNullOrWhiteSpace(request.Name) ?
					$"where { DatabaseDefinition.BuildWhereStatement(NeutralField.NeutralName, DatabaseUtility.GetWhereComparisonType(request.NameMatchStyle))}" :
					request.NeutralIds.Count() != 0 ?
						$"where { DatabaseDefinition.BuildWhereStatement(NeutralField.NeutralId, WhereStatementType.Includes)}" :
						"";

			var whereMatch = !string.IsNullOrWhiteSpace(request.Name) ?
					new (string, object)[] { (DatabaseDefinition.GetSelectResult(NeutralField.NeutralName), request.NameMatchStyle == NameMatchStyle.Similar ? $"%{request.Name}%" : request.Name) } :
					request.NeutralIds.Count() != 0 ?
						new (string, object)[] { (DatabaseDefinition.GetSelectResult(NeutralField.NeutralId), request.NeutralIds.ToArray()) } :
						new (string, object)[] { };

			reply.Neutrals.AddRange(await db.RunCommand(connector,
				$@"select {select} from {DatabaseDefinition.DefaultTableName} {joins} {where};",
				whereMatch,
				x => MapNeutral(x, request.Fields)));

			return reply;
		}

		private static Neutral MapNeutral(IDataRecord data, IReadOnlyList<NeutralField> fields)
		{
			var neutral = new Neutral();

			if (fields.Count == 0)
				fields = DatabaseDefinition.BasicFields;

			if (fields.Contains(NeutralField.NeutralId))
				neutral.Id = data.Get<int>(DatabaseDefinition.GetSelectResult(NeutralField.NeutralId));
			if (fields.Contains(NeutralField.NeutralName))
				neutral.Name = data.Get<string>(DatabaseDefinition.GetSelectResult(NeutralField.NeutralName));
			if (fields.Contains(NeutralField.NeutralGamePackageId))
				neutral.GamePackageId = data.Get<int>(DatabaseDefinition.GetSelectResult(NeutralField.NeutralGamePackageId));

			return neutral;
		}

		public static IDatabaseDefinition<NeutralField> DatabaseDefinition = new NeutralDatabaseDefinition();
	}
}
