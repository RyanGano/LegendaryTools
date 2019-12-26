using LegendaryService;
using System.Collections.Generic;
using System.Linq;
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
				await DisplayTeamsAsync(client, name: args.FirstOrDefault(), allowCloseNameMatches: true);
		}

		public static async Task DisplayTeamsAsync(GameServiceClient client, IReadOnlyList<int> teamIds = null, string name = null, bool allowCloseNameMatches = false)
		{
			var request = new GetTeamsRequest();

			if (teamIds != null && teamIds.Count() != 0)
				request.TeamIds.AddRange(teamIds);
			else if (!string.IsNullOrWhiteSpace(name))
				request.Name = name;

			request.AllowCloseNameMatches = allowCloseNameMatches;

			var teams = await client.GetTeamsAsync(request);

			foreach (var team in teams.Teams)
				ConsoleUtility.WriteLine($"{team}");
		}
	}
}
