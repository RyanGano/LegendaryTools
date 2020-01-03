using Faithlife.Utility;
using LegendaryService;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LegendaryService.GameService;

namespace LegendaryClientConsole.Utility
{
	public static class SchemeUtility
	{
		public static async Task DisplaySchemesAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				await DisplaySchemesAsync(client);
			else if (int.TryParse(args.FirstOrDefault(), out int id))
				await DisplaySchemesAsync(client, schemeIds: args.Select(x => int.Parse(x)).ToList());
			else
				await DisplaySchemesAsync(client, name: args.FirstOrDefault(), nameMatchStyle: NameMatchStyle.Similar);
		}

		public static async Task DisplaySchemesAsync(GameServiceClient client, IReadOnlyList<int> schemeIds = null, string name = null, NameMatchStyle nameMatchStyle = NameMatchStyle.MixedCase)
		{
			foreach (var scheme in await GetSchemesAsync(client, schemeIds, name, nameMatchStyle))
				ConsoleUtility.WriteLine($"{scheme}");
		}

		public static async Task<IReadOnlyList<Scheme>> GetSchemesAsync(GameServiceClient client, IReadOnlyList<int> schemeIds = null, string name = null, NameMatchStyle nameMatchStyle = NameMatchStyle.MixedCase)
		{
			var request = new GetSchemesRequest();

			if (schemeIds != null && schemeIds.Count() != 0)
				request.SchemeIds.AddRange(schemeIds);
			else if (!string.IsNullOrWhiteSpace(name))
				request.Name = name;

			request.NameMatchStyle = nameMatchStyle;

			return (await client.GetSchemesAsync(request)).Schemes;
		}

		public static async Task CreateSchemeAsync(GameServiceClient client)
		{
			var scheme = new Scheme();
			scheme.Name = ConsoleUtility.GetUserInput("Scheme Name: ");
			scheme.GamePackageId = await GamePackageUtility.SelectGamePackageId(client);
			scheme.AbilityIds.AddRange(await AbilityUtility.SelectAbilityIds(client));
			scheme.HasEpicSide = ConsoleUtility.GetUserInputBool("Has Epic side?");
			scheme.CardRequirements.AddRange(await CardRequirementUtility.GetCardRequirements(client, scheme.GamePackageId, true));
			scheme.TwistRequirements.AddRange(TwistRequirementUtility.GetTwistRequirements(client));

			if (!ConsoleUtility.ShouldContinue($"Creating Scheme: {scheme}"))
			{
				await CreateSchemeAsync(client);
				return;
			}

			var createRequest = new CreateSchemesRequest();
			createRequest.Schemes.Add(scheme);
			var createReply = await client.CreateSchemesAsync(createRequest);

			if (createReply.Status.Code != 200)
				ConsoleUtility.WriteLine($"Failed to create scheme: {createReply.Status.Message}");
			else
				ConsoleUtility.WriteLine($"Scheme '{createReply.Schemes.First().Name}' was created with Id '{createReply.Schemes.First().Id}'");
		}
	}
}
