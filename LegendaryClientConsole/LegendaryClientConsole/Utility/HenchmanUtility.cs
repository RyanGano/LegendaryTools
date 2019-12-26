using LegendaryService;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LegendaryService.GameService;

namespace LegendaryClientConsole.Utility
{
	public static class HenchmanUtility
	{
		public static async Task DisplayHenchmenAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				await DisplayHenchmenAsync(client);
			else if (int.TryParse(args.FirstOrDefault(), out int id))
				await DisplayHenchmenAsync(client, henchmanIds: args.Select(x => int.Parse(x)).ToList());
			else
				await DisplayHenchmenAsync(client, name: args.FirstOrDefault(), allowCloseNameMatches: true);
		}

		public static async Task DisplayHenchmenAsync(GameServiceClient client, IReadOnlyList<int> henchmanIds = null, string name = null, bool allowCloseNameMatches = false)
		{
			var request = new GetHenchmenRequest();

			if (henchmanIds != null && henchmanIds.Count() != 0)
				request.HenchmenIds.AddRange(henchmanIds);
			else if (!string.IsNullOrWhiteSpace(name))
				request.Name = name;

			request.AllowCloseNameMatches = allowCloseNameMatches;

			var henchmen = await client.GetHenchmenAsync(request);

			foreach (var henchman in henchmen.Henchmen)
				ConsoleUtility.WriteLine($"{henchman}");
		}
	}
}
