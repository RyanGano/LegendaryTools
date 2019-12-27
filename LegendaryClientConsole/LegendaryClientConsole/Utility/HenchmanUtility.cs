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
				await DisplayHenchmenAsync(client, name: args.FirstOrDefault(), nameMatchStyle: NameMatchStyle.Similar);
		}

		public static async Task DisplayHenchmenAsync(GameServiceClient client, IReadOnlyList<int> henchmanIds = null, string name = null, NameMatchStyle nameMatchStyle = NameMatchStyle.MixedCase)
		{
			var request = new GetHenchmenRequest();

			if (henchmanIds != null && henchmanIds.Count() != 0)
				request.HenchmanIds.AddRange(henchmanIds);
			else if (!string.IsNullOrWhiteSpace(name))
				request.Name = name;

			request.NameMatchStyle = nameMatchStyle;

			var henchmen = await client.GetHenchmenAsync(request);

			foreach (var henchman in henchmen.Henchmen)
				ConsoleUtility.WriteLine($"{henchman}");
		}
	}
}
