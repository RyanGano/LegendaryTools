using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using LegendaryService.Utility;

namespace LegendaryService
{
	public class Game : GameService.GameServiceBase
	{
		private readonly ILogger<Game> _logger;

		public Game(ILogger<Game> logger)
		{
			_logger = logger;
		}

		public override async Task<GetGamePackagesReply> GetGamePackages(GetGamePackagesRequest request, ServerCallContext context)
		{
			return await GamePackageUtility.GetGamePackagesAsync(request, context);
		}

		public override async Task<CreateGamePackageReply> CreateGamePackage(CreateGamePackageRequest request, ServerCallContext context)
		{
			return await GamePackageUtility.CreateGamePackageAsync(request, context);			
		}

		public override async Task<GetAbilitiesReply> GetAbilities(GetAbilitiesRequest request, ServerCallContext context)
		{
			return await AbilityUtility.GetAbilitiesAsync(request, context);
		}

		public override async Task<CreateAbilitiesReply> CreateAbilities(CreateAbilitiesRequest request, ServerCallContext context)
		{
			return await AbilityUtility.CreateAbilitiesAsync(request, context);
		}

		public override async Task<GetTeamsReply> GetTeams(GetTeamsRequest request, ServerCallContext context)
		{
			return await TeamUtility.GetTeamsAsync(request, context);
		}

		public override async Task<CreateTeamsReply> CreateTeams(CreateTeamsRequest request, ServerCallContext context)
		{
			return await TeamUtility.CreateTeamsAsync(request, context);
		}

		public override async Task<GetClassesReply> GetClasses(GetClassesRequest request, ServerCallContext context)
		{
			return await ClassUtility.GetClassesAsync(request, context);
		}

		public override async Task<CreateClassesReply> CreateClasses(CreateClassesRequest request, ServerCallContext context)
		{
			return await ClassUtility.CreateClassesAsync(request, context);
		}

		public override async Task<CreateHenchmenReply> CreateHenchmen(CreateHenchmenRequest request, ServerCallContext context)
		{
			return await HenchmanUtility.CreateHenchmenAsync(request, context);
		}

		public override async Task<GetHenchmenReply> GetHenchmen(GetHenchmenRequest request, ServerCallContext context)
		{
			return await HenchmanUtility.GetHenchmenAsync(request, context);
		}
		
		public override async Task<CreateAdversariesReply> CreateAdversaries(CreateAdversariesRequest request, ServerCallContext context)
		{
			return await AdversaryUtility.CreateAdversariesAsync(request, context);
		}

		public override async Task<GetAdversariesReply> GetAdversaries(GetAdversariesRequest request, ServerCallContext context)
		{
			return await AdversaryUtility.GetAdversariesAsync(request, context);
		}
	}
}
