using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using LegendaryService.Models;
using Microsoft.Extensions.Logging;
using Faithlife.Utility;
using System;
using System.Collections.Generic;
using Google.Protobuf.Collections;
using Faithlife.Data;
using System.Data;
using System.Text;

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
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var gamePackages = await connector.Command(@"
				select gp.GamePackageId, gp.Name, gp.CoverImage, pt.Name as PackageType, bm.Name as BaseMap
					from gamepackages as gp
						inner join packagetypes as pt on pt.PackageTypeId = gp.PackageTypeId
						inner join basemaps as bm on bm.BaseMapId = gp.BaseMapId;").QueryAsync(
						x => new GamePackage
						{
							Id = x.Get<int>("GamePackageId"),
							Name = x.Get<string>("Name"),
							CoverImage = x.Get<string>("CoverImage"),
							PackageType = (GamePackageType)Enum.Parse(typeof(GamePackageType), x.Get<string>("PackageType")),
							BaseMap = (GameBaseMap)Enum.Parse(typeof(GameBaseMap), x.Get<string>("BaseMap"))
						});

			if (request.Fields.Contains(GamePackageField.Abilities))
			{
				IReadOnlyList<Ability> abilities = await GetAllAbilities(gamePackages, connector);
				
				foreach (var gamePackage in gamePackages)
					gamePackage.AbilitieIds.AddRange(abilities.Where(x => x.GamePackage.Id == gamePackage.Id).Select(x => x.Id));
			}

			var reply = new GetGamePackagesReply();
			if (gamePackages.Count() != 0)
				reply.Packages.AddRange(gamePackages);
			reply.Status = new Status { Code = 200 };

			return reply;
		}

		public override async Task<CreateGamePackageReply> CreateGamePackage(CreateGamePackageRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var gamePackageId = await connector.Command(@"select GamePackageId from gamepackages where name like @Name", ("Name", request.Name)).QuerySingleOrDefaultAsync<int?>();

			if (gamePackageId.HasValue)
				return new CreateGamePackageReply { Status = new Status { Code = 400, Message = "Game Package already in database" } };

			var packageTypeId = await connector.Command(@"select PackageTypeId from packagetypes where Name = @Name;", ("Name", request.PackageType.ToString())).QuerySingleAsync<int>();
			var baseMapId = await connector.Command(@"select BaseMapId from basemaps where Name = @Name;", ("Name", request.BaseMap.ToString())).QuerySingleAsync<int>();

			await connector.Command(@"
				insert into gamepackages (Name, CoverImage, PackageTypeId, BaseMapId) values (@Name, @CoverImage, @PackageTypeId, @BaseMapId)",
				("Name", request.Name),
				("CoverImage", request.CoverImage),
				("PackageTypeId", packageTypeId),
				("BaseMapId", baseMapId)
				).QuerySingleOrDefaultAsync<int?>();

			gamePackageId = await connector.Command(@"select GamePackageId from gamepackages where name like @Name", ("Name", request.Name)).QuerySingleAsync<int>();

			if (!gamePackageId.HasValue)
				return new CreateGamePackageReply { Status = new Status { Code = 500, Message = "Item not added to database" } };

			return new CreateGamePackageReply { Id = gamePackageId.Value, Status = new Status { Code = 200 } };
		}
		public override async Task<GetAbilitiesReply> GetAbilities(GetAbilitiesRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });
			var reply = new GetAbilitiesReply { Status = new Status { Code = 200 } };

			List<Ability> abilities = new List<Ability>();

			var select = request.AbilityFields.Select(x => AbilitySqlMap[x]).Join(", ");
			
			if (request.GamePackageId > 0)
				reply.Abilities.AddRange(await connector
					.Command($@"select {select} from abilities inner join gamepackages on abilities.GamePackageId = gamepackages.GamePackageId where abilities.GamePackageId = @GamePackageId;",
						("GamePackageId", request.GamePackageId))
					.QueryAsync(x => MapAbility(x, request.AbilityFields, null)));

			return reply;
		}

		public override async Task<CreateAbilitiesReply> CreateAbilities(CreateAbilitiesRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new CreateAbilitiesReply { Status = new Status { Code = 200 } };

			var groupedAbilities = request.Abilities.GroupBy(x => x.Name.ToLower());
			if (groupedAbilities.Any(x => x.Count() != 1 && x.DistinctBy(ability => ability.GamePackage.Id).Count() == 1))
			{
				reply.Status.Code = 400;
				reply.Status.Message = $"Duplicate entries in request: " + groupedAbilities.Where(x => x.Count() != 1).Select(x => x.Key).Join(", ");
				return reply;
			}

			var existingAbilities = await GetExistingAbilities(request.Abilities, connector);
			if (existingAbilities.Count() != 0)
			{
				reply.Status.Message = $"Database already contains entries: " + existingAbilities.Select(x => x.Name).Join(", ");

				if (request.CreateOptions.Contains(CreateOptions.ErrorOnDuplicates))
				{
					reply.Status.Code = 400;
					return reply;
				}
			}

			var abilitiesToInsert = request.Abilities.Where(x => !existingAbilities.Any(existingAbility => existingAbility.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase))).Select(x => new { x.Name, x.Description, GamePackageId = x.GamePackage.Id }).ToList();

			using var transaction = await connector.BeginTransactionAsync();
			
			foreach (var abilityToInsert in abilitiesToInsert)
				await connector.Command(@"insert into abilities (Name, Description, GamePackageId) values (@Name, @Description, @GamePackageId);", DbParameters.FromDto(abilityToInsert)).ExecuteAsync();

			await connector.CommitTransactionAsync();

			existingAbilities = await GetExistingAbilities(request.Abilities, connector);

			reply.Abilities.AddRange(existingAbilities);
			return reply;
		}
		private async ValueTask<IReadOnlyList<Ability>> GetAllAbilities(IReadOnlyList<GamePackage> gamePackages, DbConnector connector)
		{
			var test = (await connector.Command(@"select * from abilities;").QueryAsync(x => MapAbility(x, BasicAbilityFields, gamePackages))).ToList();
			return test;
		}

		private async ValueTask<IReadOnlyList<Ability>> GetExistingAbilities(RepeatedField<Ability> abilities, DbConnector connector)
		{
			var foundAbilities = new List<Ability>();

			var gamePackages = abilities.Select(x => x.GamePackage).DistinctBy(x => x.Id).ToList();

			foreach (var gamePackageAbilities in abilities.GroupBy(x => x.GamePackage.Id))
			{
				foundAbilities.AddRange(await connector.Command(@"select * from abilities where name REGEXP @Names and GamePackageId = @GamePackageId;",
					("Names", gamePackageAbilities.Select(x => LegendaryDatabase.Encode(x.Name)).Join("|")),
					("GamePackageId", gamePackageAbilities.Key))
					.QueryAsync(x => MapAbility(x, BasicAbilityFields, gamePackages)));
			}

			return foundAbilities;
		}

		static readonly IReadOnlyList<AbilityField> BasicAbilityFields = new[]
		{
			AbilityField.Id,
			AbilityField.Name,
			AbilityField.Description,
			AbilityField.GamePackageId
		};

		private Ability MapAbility(IDataRecord data, IReadOnlyList<AbilityField> fields, IReadOnlyList<GamePackage> gamePackages)
		{
			var gamePackage = gamePackages?.FirstOrDefault(x => x.Id == data.Get<int>("GamePackageId"));

			if (gamePackage == null && (fields.Contains(AbilityField.GamePackageId) || fields.Contains(AbilityField.GamePackageName)))
			{
				gamePackage = new GamePackage();

				if (fields.Contains(AbilityField.GamePackageId))
					gamePackage.Id = data.Get<int>("gamepackages.GamePackageId");
			}


			return new Ability
			{
				Id = fields.Contains(AbilityField.Id) ? data.Get<int>("AbilityId") : 0,
				Name = fields.Contains(AbilityField.Name) ? data.Get<string>("Name") : "",
				Description = fields.Contains(AbilityField.Description) ? data.Get<string>("Description") : "",
				GamePackage = gamePackage
			};
		}

		readonly List<GamePackage> m_gamePackages = new List<GamePackage>();

		private readonly Dictionary<AbilityField, string> AbilitySqlMap = new Dictionary<AbilityField, string>
		{
			{ AbilityField.Id, "abilities.AbilityId" },
			{ AbilityField.Name, "abilities.Name" },
			{ AbilityField.Description, "abilities.Description" },
			{ AbilityField.GamePackageId, "abilities.GamePackageId" },
			{ AbilityField.GamePackageName, "gamepackages.Name" },
		};
	}
}
