using Faithlife.Data;
using Faithlife.Utility;
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
	public static class TeamUtility
	{
		public static async Task<CreateTeamsReply> CreateTeamsAsync(CreateTeamsRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new CreateTeamsReply { Status = new Status { Code = 200 } };

			var teams = await GetTeamsAsync(new GetTeamsRequest(), context);

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
						into {DatabaseDefinition.DefaultTableName}
							({DatabaseDefinition.ColumnName[TeamField.TeamName]}, {DatabaseDefinition.ColumnName[TeamField.TeamImagePath]})
						values (@TeamName, @ImagePath);
					select last_insert_id();",
					("TeamName", team.Name),
					("ImagePath", team.ImagePath))
					.QuerySingleAsync<ulong>()));
			}

			var finalTeamsList = new GetTeamsRequest();
			finalTeamsList.TeamIds.AddRange(insertIds);
			var createdTeams = await GetTeamsAsync(finalTeamsList, context);

			reply.Teams.AddRange(createdTeams.Teams);
			return reply;
		}

		public static async Task<GetTeamsReply> GetTeamsAsync(GetTeamsRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new GetTeamsReply { Status = new Status { Code = 200 } };

			var select = DatabaseDefinition.BuildSelectStatement(request.Fields);
			var joins = DatabaseDefinition.BuildRequiredJoins(request.Fields);

			var where = !string.IsNullOrWhiteSpace(request.Name) ?
					$"where { DatabaseDefinition.BuildWhereStatement(TeamField.TeamName, WhereStatementType.Like)}" :
					request.TeamIds.Count() != 0 ?
						$"where { DatabaseDefinition.BuildWhereStatement(TeamField.TeamId, WhereStatementType.Includes)}" :
						"";

			var whereMatch = !string.IsNullOrWhiteSpace(request.Name) ?
					new (string, object)[] { (DatabaseDefinition.GetSelectResult(TeamField.TeamName), $"%{request.Name}%") } :
					request.TeamIds.Count() != 0 ?
						new (string, object)[] { (DatabaseDefinition.GetSelectResult(TeamField.TeamId), request.TeamIds.ToArray()) } :
						new (string, object)[] { };

			reply.Teams.AddRange(await db.RunCommand(connector,
				$@"select {select} from {DatabaseDefinition.DefaultTableName} {joins} {where};",
				whereMatch,
				x => MapTeam(x, request.Fields)));

			return reply;
		}

		private static Team MapTeam(IDataRecord data, IReadOnlyList<TeamField> fields)
		{
			var team = new Team();

			if (fields.Count == 0)
				fields = DatabaseDefinition.BasicFields;

			if (fields.Contains(TeamField.TeamId))
				team.Id = data.Get<int>(DatabaseDefinition.GetSelectResult(TeamField.TeamId));
			if (fields.Contains(TeamField.TeamName))
				team.Name = data.Get<string>(DatabaseDefinition.GetSelectResult(TeamField.TeamName));
			if (fields.Contains(TeamField.TeamImagePath))
				team.ImagePath = data.Get<string>(DatabaseDefinition.GetSelectResult(TeamField.TeamImagePath));

			return team;
		}

		public static IDatabaseDefinition<TeamField> DatabaseDefinition = new TeamDatabaseDefinition();
	}
}
