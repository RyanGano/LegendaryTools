using LegendaryService;
using System.Collections.Generic;
using static LegendaryService.GameService;

namespace LegendaryClientConsole.Utility
{
	internal static class TwistRequirementUtility
	{
		internal static IEnumerable<SchemeTwistRequirement> GetTwistRequirements(GameServiceClient client)
		{
			List<SchemeTwistRequirement> twistRequirements = new List<SchemeTwistRequirement>();

			twistRequirements.AddRange(GetSingleTwistRequirement(client));

			return twistRequirements;
		}
		internal static IEnumerable<SchemeTwistRequirement> GetSingleTwistRequirement(GameServiceClient client)
		{
			var changesBasedOnPlayerCount = ConsoleUtility.GetUserInputBool("Does the twist requirement change based on the number of players? ", null);

			if (!changesBasedOnPlayerCount)
			{
				int twistCount = ConsoleUtility.GetUserInputInt("How many twists? ");

				yield return new SchemeTwistRequirement
				{
					SchemeTwistCount = twistCount,
					Allowed = true
				};
			}
			else
			{
				bool schemeAllowedForSinglePlayer = ConsoleUtility.GetUserInputBool($"Is this scheme allowed for a single player? ");

				if (!schemeAllowedForSinglePlayer)
				{
					yield return new SchemeTwistRequirement
					{
						PlayerCount = 1,
						Allowed = false
					};
				}

				for (int i = schemeAllowedForSinglePlayer ? 1 : 2; i <= 5; i++)
				{
					var playerMessage = i == 1 ? "player" : "players";
					int twistCount = ConsoleUtility.GetUserInputInt($"How many twists for {i} {playerMessage}? ");

					yield return new SchemeTwistRequirement
					{
						PlayerCount = i,
						SchemeTwistCount = twistCount,
						Allowed = true
					};
				}
			}
		}
	}
}