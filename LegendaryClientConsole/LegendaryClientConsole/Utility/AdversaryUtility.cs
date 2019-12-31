using LegendaryService;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LegendaryService.GameService;

namespace LegendaryClientConsole.Utility
{
	public static class AdversaryUtility
	{
		public static async Task DisplayAdversariesAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				await DisplayAdversariesAsync(client);
			else if (int.TryParse(args.FirstOrDefault(), out int id))
				await DisplayAdversariesAsync(client, adversaryIds: args.Select(x => int.Parse(x)).ToList());
			else
				await DisplayAdversariesAsync(client, name: args.FirstOrDefault(), nameMatchStyle: NameMatchStyle.Similar);
		}

		public static async Task DisplayAdversariesAsync(GameServiceClient client, IReadOnlyList<int> adversaryIds = null, string name = null, NameMatchStyle nameMatchStyle = NameMatchStyle.MixedCase)
		{
			foreach (var adversary in await GetAdversariesAsync(client, adversaryIds, name, nameMatchStyle))
				ConsoleUtility.WriteLine($"{adversary}");
		}

		public static async Task<IReadOnlyList<Adversary>> GetAdversariesAsync(GameServiceClient client, IReadOnlyList<int> adversaryIds = null, string name = null, NameMatchStyle nameMatchStyle = NameMatchStyle.MixedCase)
		{
			var request = new GetAdversariesRequest();

			if (adversaryIds != null && adversaryIds.Count() != 0)
				request.AdversaryIds.AddRange(adversaryIds);
			else if (!string.IsNullOrWhiteSpace(name))
				request.Name = name;

			request.NameMatchStyle = nameMatchStyle;

			return (await client.GetAdversariesAsync(request)).Adversaries;
		}
	}
}
