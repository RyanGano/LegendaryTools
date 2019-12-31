using LegendaryService;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LegendaryService.GameService;

namespace LegendaryClientConsole.Utility
{
	public static class NeutralUtility
	{
		public static async Task DisplayNeutralsAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				await DisplayNeutralsAsync(client);
			else if (int.TryParse(args.FirstOrDefault(), out int id))
				await DisplayNeutralsAsync(client, neutralIds: args.Select(x => int.Parse(x)).ToList());
			else
				await DisplayNeutralsAsync(client, name: args.FirstOrDefault(), nameMatchStyle: NameMatchStyle.Similar);
		}

		public static async Task DisplayNeutralsAsync(GameServiceClient client, IReadOnlyList<int> neutralIds = null, string name = null, NameMatchStyle nameMatchStyle = NameMatchStyle.MixedCase)
		{
			foreach (var neutral in await GetNeutralsAsync(client, neutralIds, name, nameMatchStyle))
				ConsoleUtility.WriteLine($"{neutral}");
		}

		public static async Task<IReadOnlyList<Neutral>> GetNeutralsAsync(GameServiceClient client, IReadOnlyList<int> neutralIds = null, string name = null, NameMatchStyle nameMatchStyle = NameMatchStyle.MixedCase)
		{
			var request = new GetNeutralsRequest();

			if (neutralIds != null && neutralIds.Count() != 0)
				request.NeutralIds.AddRange(neutralIds);
			else if (!string.IsNullOrWhiteSpace(name))
				request.Name = name;

			request.NameMatchStyle = nameMatchStyle;

			return (await client.GetNeutralsAsync(request)).Neutrals;
		}
	}
}
