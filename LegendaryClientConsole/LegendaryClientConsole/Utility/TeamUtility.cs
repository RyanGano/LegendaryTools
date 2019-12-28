using Faithlife.Utility;
using LegendaryService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static LegendaryService.GameService;

namespace LegendaryClientConsole.Utility
{
	public static class TeamUtility
	{
		public static async Task DisplayTeamsAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				await DisplayTeamsAsync(client);
			else if (int.TryParse(args.FirstOrDefault(), out int id))
				await DisplayTeamsAsync(client, teamIds: args.Select(x => int.Parse(x)).ToList());
			else
				await DisplayTeamsAsync(client, name: args.FirstOrDefault(), nameMatchStyle: NameMatchStyle.Similar);
		}

		public static async Task DisplayTeamsAsync(GameServiceClient client, IReadOnlyList<int> teamIds = null, string name = null, NameMatchStyle nameMatchStyle = NameMatchStyle.MixedCase)
		{
			var request = new GetTeamsRequest();

			if (teamIds != null && teamIds.Count() != 0)
				request.TeamIds.AddRange(teamIds);
			else if (!string.IsNullOrWhiteSpace(name))
				request.Name = name;

			request.NameMatchStyle = nameMatchStyle;

			var teams = await client.GetTeamsAsync(request);

			foreach (var team in teams.Teams)
				ConsoleUtility.WriteLine($"{team}");
		}

		public static async Task<int> SelectTeamId(GameServiceClient client)
		{
			var teamId = 0;
			IReadOnlyList<Team> teams = null;

			while (teamId == 0)
			{
				var input = ConsoleUtility.GetUserInput("What team is this entry associated with (? to see listing): ");
				if (input == "?")
				{
					teams = await DisplayTeamsSimpleAsync(client, teams);
				}
				else if (!string.IsNullOrWhiteSpace(input))
				{
					teams = await GetTeamsAsync(client, teams);

					if (int.TryParse(input, out int id))
					{
						teamId = teams.Select(x => x.Id).FirstOrDefault(x => x == id, 0);
						if (teamId == 0)
							ConsoleUtility.WriteLine($"Team Id '{input}' was not found");
					}
					else
					{
						var matchingTeams = teams.Where(x => Regex.IsMatch(x.Name.ToLower(), input.ToLower())).ToList();
						if (matchingTeams.Count == 0)
							ConsoleUtility.WriteLine($"Team Name '{input}' was not found");
						else if (matchingTeams.Count != 1)
							ConsoleUtility.WriteLine($"Team Name '{input}' matched multiple Teams ({matchingTeams.Select(x => x.Name).Join(", ")})");
						else
							teamId = matchingTeams.First().Id;
					}
				}

				if (teamId != 0)
				{
					var team = teams.First(x => x.Id == teamId);
					if (!ConsoleUtility.ShouldContinue($"Adding entry to team '{team.Id}: {team.Name}':"))
						teamId = 0;
				}
			}

			return teamId;
		}

		public static async ValueTask<IReadOnlyList<Team>> DisplayTeamsSimpleAsync(GameServiceClient client, IReadOnlyList<Team> teams)
		{
			teams = await GetTeamsAsync(client, teams);

			foreach (var team in teams)
				ConsoleUtility.WriteLine($"{team.Id}: {team.Name}");

			return teams;
		}

		public static async Task<IReadOnlyList<Team>> GetTeamsAsync(GameServiceClient client, IReadOnlyList<Team> teams)
		{
			if (teams == null)
			{
				var request = new GetTeamsRequest();
				request.Fields.AddRange(new[] { TeamField.TeamId, TeamField.TeamName });
				teams = (await client.GetTeamsAsync(request)).Teams.ToList();
			}

			return teams;
		}
	}
}
