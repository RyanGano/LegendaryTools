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
	public static class AllyUtility
	{
		public static async Task<CreateAlliesReply> CreateAlliesAsync(CreateAlliesRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new CreateAlliesReply { Status = new Status { Code = 200 } };

			List<int> newAllyIds = new List<int>();

			foreach (var ally in request.Allies)
			{
				// Validate the GamePackageId
				var packageRequest = new GetGamePackagesRequest();
				packageRequest.GamePackageIds.Add(ally.GamePackageId);
				packageRequest.Fields.Add(GamePackageField.Id);
				var packageReply = await GamePackageUtility.GetGamePackagesAsync(packageRequest, context);
				if (packageReply.Status.Code != 200)
				{
					reply.Status = packageReply.Status;
					return reply;
				}

				// Validate the AbilityIds
				var abilitiesRequest = new GetAbilitiesRequest();
				abilitiesRequest.AbilityIds.AddRange(ally.AbilityIds);
				abilitiesRequest.AbilityFields.Add(AbilityField.Id);
				var abilitiesReply = await AbilityUtility.GetAbilitiesAsync(abilitiesRequest, context);
				if (abilitiesReply.Status.Code != 200)
				{
					reply.Status = abilitiesReply.Status;
					return reply;
				}

				// Verify that this ally doesn't already exist
				var allyRequest = new GetAlliesRequest();
				allyRequest.Name = ally.Name;
				allyRequest.Fields.AddRange(new[] { AllyField.AllyId, AllyField.AllyName, AllyField.AllyGamePackageId});
				allyRequest.NameMatchStyle = NameMatchStyle.MixedCase;
				var allyReply = await GetAlliesAsync(allyRequest, context);
				if (allyReply.Status.Code == 200 && allyReply.Allies.Any())
				{
					var matchingAlly = allyReply.Allies.First();
					reply.Status = new Status { Code = 400, Message = $"Ally {matchingAlly.Id} with name '{matchingAlly.Name}' was found in game package '{matchingAlly.GamePackageId}'" };
					return reply;
				}

				// Verify that the class counts add up to at least 14
				if (ally.Classes.Select(x => x.Count).Sum() < 14)
				{
					reply.Status = new Status { Code = 400, Message = $"Ally with name '{ally.Name}' must be supplied with at least 14 class cards." };
					return reply;
				}

				// Verify that the classIds are valid
				var classesRequest = new GetClassesRequest();
				classesRequest.ClassIds.AddRange(ally.Classes.Select(x => x.ClassId));
				classesRequest.Fields.Add(ClassField.ClassId);
				var classesReply = await ClassUtility.GetClassesAsync(classesRequest, context);
				if (classesReply.Status.Code != 200)
				{
					reply.Status = classesReply.Status;
					return reply;
				}

				// Create the ally
				var newAllyId = ((int)(await connector.Command($@"
					insert
						into {DatabaseDefinition.DefaultTableName}
							({DatabaseDefinition.ColumnName[AllyField.AllyName]}, {DatabaseDefinition.ColumnName[AllyField.AllyTeamId]})
									values (@AllyName, @AllyTeamId);
								select last_insert_id();",
				("AllyName", ally.Name),
				("AllyTeamId", ally.TeamId))
				.QuerySingleAsync<ulong>()));

				// Add to game package
				await connector.Command(
					$@"
						insert
							into {TableNames.GamePackageAllies}
								({DatabaseDefinition.ColumnName[AllyField.AllyId]}, {GamePackageUtility.DatabaseDefinition.ColumnName[GamePackageField.Id]})
							values (@AllyId, @GamePackageId);",
					("AllyId", newAllyId),
					("GamePackageId", ally.GamePackageId))
				.ExecuteAsync();

				// Link abilities
				foreach (var abilityId in ally.AbilityIds)
				{
					await connector.Command(
						$@"
							insert
								into {TableNames.AllyAbilities}
									({DatabaseDefinition.ColumnName[AllyField.AllyId]}, {AbilityUtility.DatabaseDefinition.ColumnName[AbilityField.Id]})
								values (@AllyId, @AbilityId);",
						("AllyId", newAllyId),
						("AbilityId", abilityId))
					.ExecuteAsync();
				}

				// Add class info
				foreach (var classInfo in ally.Classes)
				{
					await connector.Command(
						$@"
							insert
								into {TableNames.AllyClasses}
									({DatabaseDefinition.ColumnName[AllyField.AllyId]}, {ClassUtility.DatabaseDefinition.ColumnName[ClassField.ClassId]}, CardCount)
								values (@AllyId, @ClassId, @CardCount);",
						("AllyId", newAllyId),
						("ClassId", classInfo.ClassId),
						("CardCount", classInfo.Count))
					.ExecuteAsync();
				}

				newAllyIds.Add(newAllyId);
			}

			// Get all of the created ally
			var finalRequest = new GetAlliesRequest();
			finalRequest.AllyIds.AddRange(newAllyIds);
			var finalReply = await GetAlliesAsync(finalRequest, context);

			reply.Status = finalReply.Status;
			reply.Allies.AddRange(finalReply.Allies);

			return reply;
		}

		public static async Task<GetAlliesReply> GetAlliesAsync(GetAlliesRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new GetAlliesReply { Status = new Status { Code = 200 } };

			if (request.Fields.Count() == 0)
				request.Fields.AddRange(DatabaseDefinition.BasicFields);

			// Need to remove abilityIds field because it's handled separately from the main db request
			var includeAbilityIds = request.Fields.Remove(AllyField.AllyAbilityIds);
			var includeClassInfo = request.Fields.Remove(AllyField.AllyClassInfo);

			var select = DatabaseDefinition.BuildSelectStatement(request.Fields);
			var joins = DatabaseDefinition.BuildRequiredJoins(request.Fields);

			var where = !string.IsNullOrWhiteSpace(request.Name) ?
					$"where { DatabaseDefinition.BuildWhereStatement(AllyField.AllyName, DatabaseUtility.GetWhereComparisonType(request.NameMatchStyle))}" :
					request.AllyIds.Count() != 0 ?
						$"where { DatabaseDefinition.BuildWhereStatement(AllyField.AllyId, WhereStatementType.Includes)}" :
						"";

			var whereMatch = !string.IsNullOrWhiteSpace(request.Name) ?
					new (string, object)[] { (DatabaseDefinition.GetSelectResult(AllyField.AllyName), request.NameMatchStyle == NameMatchStyle.Similar ? $"%{request.Name}%" : request.Name) } :
					request.AllyIds.Count() != 0 ?
						new (string, object)[] { (DatabaseDefinition.GetSelectResult(AllyField.AllyId), request.AllyIds.ToArray()) } :
						new (string, object)[] { };

			reply.Allies.AddRange(await db.RunCommand(connector,
				$@"select {select} from {DatabaseDefinition.DefaultTableName} {joins} {where};",
				whereMatch,
				x => MapAlly(x, request.Fields)));

			if (includeAbilityIds)
			{
				// Lookup the abilities for each ally
				foreach (var ally in reply.Allies)
				{
					var abilitySelect = "AbilityId";
					ally.AbilityIds.AddRange(await connector.Command($@"
						select {abilitySelect} from {TableNames.AllyAbilities} where AllyId = @AllyId;", ("AllyId", ally.Id)).QueryAsync<int>());
				}
			}

			if (includeClassInfo)
			{
				// Lookup the class info for each ally
				foreach (var ally in reply.Allies)
				{
					var classSelect = "ClassId, CardCount";
					ally.Classes.AddRange(await connector.Command($@"
						select {classSelect} from {TableNames.AllyClasses} where AllyId = @AllyId;", ("AllyId", ally.Id)).QueryAsync<ClassInfo>(MapClassInfo));
				}
			}

			return reply;
		}

		private static Ally MapAlly(IDataRecord data, IReadOnlyList<AllyField> fields)
		{
			var ally = new Ally();

			if (fields.Count == 0)
				fields = DatabaseDefinition.BasicFields;

			if (fields.Contains(AllyField.AllyId))
				ally.Id = data.Get<int>(DatabaseDefinition.GetSelectResult(AllyField.AllyId));
			if (fields.Contains(AllyField.AllyName))
				ally.Name = data.Get<string>(DatabaseDefinition.GetSelectResult(AllyField.AllyName));
			if (fields.Contains(AllyField.AllyGamePackageId))
				ally.GamePackageId = data.Get<int>(DatabaseDefinition.GetSelectResult(AllyField.AllyGamePackageId));
			if (fields.Contains(AllyField.AllyTeamId))
				ally.TeamId = data.Get<int>(DatabaseDefinition.GetSelectResult(AllyField.AllyTeamId));

			return ally;
		}

		private static ClassInfo MapClassInfo(IDataRecord data)
		{
			var classInfo = new ClassInfo();
			classInfo.ClassId = data.Get<int>("ClassId");
			classInfo.Count = data.Get<int>("CardCount");

			return classInfo;
		}
		
		public static IDatabaseDefinition<AllyField> DatabaseDefinition = new AllyDatabaseDefinition();
	}
}
