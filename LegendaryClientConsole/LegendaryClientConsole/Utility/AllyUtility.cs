using LegendaryService;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LegendaryService.GameService;

namespace LegendaryClientConsole.Utility
{
	public static class AllyUtility
	{
		public static async Task DisplayAlliesAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				await DisplayAlliesAsync(client);
			else if (int.TryParse(args.FirstOrDefault(), out int id))
				await DisplayAlliesAsync(client, allyIds: args.Select(x => int.Parse(x)).ToList());
			else
				await DisplayAlliesAsync(client, name: args.FirstOrDefault(), nameMatchStyle: NameMatchStyle.Similar);
		}

		public static async Task DisplayAlliesAsync(GameServiceClient client, IReadOnlyList<int> allyIds = null, string name = null, NameMatchStyle nameMatchStyle = NameMatchStyle.MixedCase)
		{
			foreach (var ally in await GetAlliesAsync(client, allyIds, name, nameMatchStyle))
				ConsoleUtility.WriteLine($"{ally}");
		}

		public static async Task<IReadOnlyList<Ally>> GetAlliesAsync(GameServiceClient client, IReadOnlyList<int> allyIds = null, string name = null, NameMatchStyle nameMatchStyle = NameMatchStyle.MixedCase)
		{
			var request = new GetAlliesRequest();

			if (allyIds != null && allyIds.Count() != 0)
				request.AllyIds.AddRange(allyIds);
			else if (!string.IsNullOrWhiteSpace(name))
				request.Name = name;

			request.NameMatchStyle = nameMatchStyle;

			return (await client.GetAlliesAsync(request)).Allies;
		}
	}
}
