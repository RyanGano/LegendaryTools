using Faithlife.Utility;
using LegendaryService;
using System.Linq;
using System.Threading.Tasks;
using static LegendaryService.GameService;

namespace LegendaryClientConsole.Utility
{
	public static class CreateUtility
	{
		public static async Task CreateItemAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				ConsoleUtility.WriteLine("Must supply the type of item you want to create. (t|h)");
			else if (args.FirstOrDefault() == "t")
				await CreateTeamAsync(client);
			else if (args.FirstOrDefault() == "h")
				await CreateHenchmanAsync(client);
			else if (args.FirstOrDefault() == "n")
				await CreateNeutralAsync(client);
		}

		public static async Task CreateTeamAsync(GameServiceClient client)
		{
			var teamName = ConsoleUtility.GetUserInput("Team Name: ");
			var imagePath = ConsoleUtility.GetUserInput("Path to Image (on OneDrive): ");

			var createRequest = new CreateTeamsRequest();
			createRequest.Teams.Add(new Team { Name = teamName, ImagePath = imagePath });
			createRequest.CreateOptions.Add(CreateOptions.ErrorOnDuplicates);

			var reply = await client.CreateTeamsAsync(createRequest);

			if (reply.Status.Code != 200)
				ConsoleUtility.WriteLine(reply.Status.Message);
			else
				ConsoleUtility.WriteLine($"Team '{reply.Teams.First().Name}' was created with Id '{reply.Teams.First().Id}'");
		}

		public static async Task CreateHenchmanAsync(GameServiceClient client)
		{
			var henchman = new Henchman();
			henchman.Name = ConsoleUtility.GetUserInput("Henchman Name: ");
			henchman.GamePackageId = await GamePackageUtility.SelectGamePackageId(client);
			henchman.AbilityIds.AddRange(await AbilityUtility.SelectAbilityIds(client));

			if (!ConsoleUtility.ShouldContinue($"Creating Henchman: '{henchman.Name}', in gamePackage '{henchman.GamePackageId}' with abilities [{henchman.AbilityIds.Select(x => x.ToString()).Join(", ")}]"))
			{
				await CreateHenchmanAsync(client);
				return;
			}

			var createRequest = new CreateHenchmenRequest();
			createRequest.Henchmen.Add(henchman);
			var createReply = await client.CreateHenchmenAsync(createRequest);

			if (createReply.Status.Code != 200)
				ConsoleUtility.WriteLine($"Failed to create henchman: {createReply.Status.Message}");
			else
				ConsoleUtility.WriteLine($"Team '{createReply.Henchmen.First().Name}' was created with Id '{createReply.Henchmen.First().Id}'");
		}

		public static async Task CreateNeutralAsync(GameServiceClient client)
		{
			var neutral = new Neutral();
			neutral.Name = ConsoleUtility.GetUserInput("Neutral Name: ");
			neutral.GamePackageId = await GamePackageUtility.SelectGamePackageId(client);

			if (!ConsoleUtility.ShouldContinue($"Creating Neutral: '{neutral.Name}', in gamePackage '{neutral.GamePackageId}'"))
			{
				await CreateNeutralAsync(client);
				return;
			}

			var createRequest = new CreateNeutralsRequest();
			createRequest.Neutrals.Add(neutral);
			var createReply = await client.CreateNeutralsAsync(createRequest);

			if (createReply.Status.Code != 200)
				ConsoleUtility.WriteLine($"Failed to create neutral: {createReply.Status.Message}");
			else
				ConsoleUtility.WriteLine($"Team '{createReply.Neutrals.First().Name}' was created with Id '{createReply.Neutrals.First().Id}'");
		}
	}
}
