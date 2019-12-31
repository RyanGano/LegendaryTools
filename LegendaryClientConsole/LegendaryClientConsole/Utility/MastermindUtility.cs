using Faithlife.Utility;
using LegendaryService;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LegendaryService.GameService;

namespace LegendaryClientConsole.Utility
{
	public static class MastermindUtility
	{
		public static async Task DisplayMastermindsAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				await DisplayMastermindsAsync(client);
			else if (int.TryParse(args.FirstOrDefault(), out int id))
				await DisplayMastermindsAsync(client, mastermindIds: args.Select(x => int.Parse(x)).ToList());
			else
				await DisplayMastermindsAsync(client, name: args.FirstOrDefault(), nameMatchStyle: NameMatchStyle.Similar);
		}

		public static async Task DisplayMastermindsAsync(GameServiceClient client, IReadOnlyList<int> mastermindIds = null, string name = null, NameMatchStyle nameMatchStyle = NameMatchStyle.MixedCase)
		{
			foreach (var mastermind in await GetMastermindsAsync(client, mastermindIds, name, nameMatchStyle))
				ConsoleUtility.WriteLine($"{mastermind}");
		}

		public static async Task<IReadOnlyList<Mastermind>> GetMastermindsAsync(GameServiceClient client, IReadOnlyList<int> mastermindIds = null, string name = null, NameMatchStyle nameMatchStyle = NameMatchStyle.MixedCase)
		{
			var request = new GetMastermindsRequest();

			if (mastermindIds != null && mastermindIds.Count() != 0)
				request.MastermindIds.AddRange(mastermindIds);
			else if (!string.IsNullOrWhiteSpace(name))
				request.Name = name;

			request.NameMatchStyle = nameMatchStyle;

			return (await client.GetMastermindsAsync(request)).Masterminds;
		}

		public static async Task CreateMastermindAsync(GameServiceClient client)
		{
			var mastermind = new Mastermind();
			mastermind.Name = ConsoleUtility.GetUserInput("Mastermind Name: ");
			mastermind.GamePackageId = await GamePackageUtility.SelectGamePackageId(client);
			mastermind.AbilityIds.AddRange(await AbilityUtility.SelectAbilityIds(client));
			mastermind.HasEpicSide = ConsoleUtility.GetUserInputBool("Has Epic side?");
			mastermind.CardRequirements.AddRange(await CardRequirementUtility.GetCardRequirements(client, mastermind.GamePackageId, true));

			if (!ConsoleUtility.ShouldContinue($"Creating Mastermind: {mastermind}"))
			{
				await CreateMastermindAsync(client);
				return;
			}

			var createRequest = new CreateMastermindsRequest();
			createRequest.Masterminds.Add(mastermind);
			var createReply = await client.CreateMastermindsAsync(createRequest);

			if (createReply.Status.Code != 200)
				ConsoleUtility.WriteLine($"Failed to create mastermind: {createReply.Status.Message}");
			else
				ConsoleUtility.WriteLine($"Mastermind '{createReply.Masterminds.First().Name}' was created with Id '{createReply.Masterminds.First().Id}'");
		}
	}
}
