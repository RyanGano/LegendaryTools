using LegendaryService;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LegendaryService.GameService;

namespace LegendaryClientConsole.Utility
{
	public static class ClassUtility
	{
		public static async Task DisplayClassesAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				await DisplayClassesAsync(client);
			else if (int.TryParse(args.FirstOrDefault(), out int id))
				await DisplayClassesAsync(client, classIds: args.Select(x => int.Parse(x)).ToList());
			else
				await DisplayClassesAsync(client, name: args.FirstOrDefault(), allowCloseNameMatches: true);
		}

		public static async Task DisplayClassesAsync(GameServiceClient client, IReadOnlyList<int> classIds = null, string name = null, bool allowCloseNameMatches = false)
		{
			var request = new GetClassesRequest();

			if (classIds != null && classIds.Count() != 0)
				request.ClassIds.AddRange(classIds);
			else if (!string.IsNullOrWhiteSpace(name))
				request.Name = name;

			request.AllowCloseNameMatches = allowCloseNameMatches;

			var classes = await client.GetClassesAsync(request);

			foreach (var @class in classes.Classes)
				ConsoleUtility.WriteLine($"{@class}");
		}
	}
}
