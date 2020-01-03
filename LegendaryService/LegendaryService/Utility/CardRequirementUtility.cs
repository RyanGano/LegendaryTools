using Faithlife.Data;
using LegendaryService.Database;
using LegendaryService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LegendaryService.Utility
{
	public static class CardRequirementUtility
	{
		internal static async ValueTask<int> AddCardRequirement(CardRequirement requirement)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			// Create new card requirement
			var insertColumnInfo = GetInsertColumnValue(requirement);
			var insertCardTypeInfo = await GetInsertCardTypeValue(requirement, connector);
			var query = $@"insert into {TableNames.CardRequirements} ({insertColumnInfo.Name}, {insertCardTypeInfo.Name}) values (@{insertColumnInfo.Name}, @{insertCardTypeInfo.Name}); select last_insert_id();";

			return (int)await connector.Command(query,
				insertColumnInfo,
				insertCardTypeInfo)
				.QuerySingleAsync<ulong>();
		}

		internal static async ValueTask<IReadOnlyList<CardRequirement>> GetCardRequirementsAsync(int ownerId)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var fields = DatabaseDefinition.BasicFields;

			var numberOfPlayersResult = $"{TableNames.MatchedCardRequirements}_NumberOfPlayers";

			var selectStatement = DatabaseDefinition.BuildSelectStatement(fields);
			var joinStatement = DatabaseDefinition.BuildRequiredJoins(fields);
			var whereStatement = $"where {DatabaseDefinition.BuildWhereStatement(CardRequirement.OwnerIdFieldNumber, WhereStatementType.Equals)}";

			// Create new card requirement
			return (await connector.Command($@"
				select {selectStatement}
					from {DatabaseDefinition.DefaultTableName}
					{joinStatement}
					{whereStatement};",
					(DatabaseDefinition.GetSelectResult(CardRequirement.OwnerIdFieldNumber), ownerId))
				.QueryAsync(x => MapCardRequirement(x, DatabaseDefinition.BasicFields)));
		}

		private static CardRequirement MapCardRequirement(IDataRecord data, IReadOnlyList<int> fields)
		{
			var cardRequirement = new CardRequirement();

			if (fields.Count == 0)
				fields = DatabaseDefinition.BasicFields;

			if (fields.Contains(CardRequirement.CardRequirementIdFieldNumber))
				cardRequirement.CardRequirementId = data.Get<int>(DatabaseDefinition.GetSelectResult(CardRequirement.CardRequirementIdFieldNumber));
			if (fields.Contains(CardRequirement.RequiredSetIdFieldNumber))
				cardRequirement.RequiredSetId = data.Get<int>(DatabaseDefinition.GetSelectResult(CardRequirement.RequiredSetIdFieldNumber));
			if (fields.Contains(CardRequirement.AdditionalSetCountFieldNumber))
				cardRequirement.AdditionalSetCount = data.Get<int>(DatabaseDefinition.GetSelectResult(CardRequirement.AdditionalSetCountFieldNumber));
			if (fields.Contains(CardRequirement.RequiredSetNameFieldNumber))
				cardRequirement.RequiredSetName = data.Get<string>(DatabaseDefinition.GetSelectResult(CardRequirement.RequiredSetNameFieldNumber)) ?? cardRequirement.RequiredSetName;
			if (fields.Contains(CardRequirement.CardSetTypeFieldNumber))
				cardRequirement.CardSetType = s_cardSetTypeIdMap[data.Get<int>(DatabaseDefinition.GetSelectResult(CardRequirement.CardSetTypeFieldNumber))];
			if (fields.Contains(CardRequirement.PlayerCountFieldNumber))
				cardRequirement.PlayerCount = data.Get<int>($"{TableNames.MatchedCardRequirements}_NumberOfPlayers");

			return cardRequirement;
		}

		private static (string Name, object Value) GetInsertColumnValue(CardRequirement requirement)
		{
			if (requirement.CardRequirementType == CardRequirementType.AdditionalSetCount)
				return (DatabaseDefinition.ColumnName[CardRequirement.AdditionalSetCountFieldNumber], requirement.AdditionalSetCount);
			if (requirement.CardRequirementType == CardRequirementType.SpecificRequiredSet)
				return (DatabaseDefinition.ColumnName[CardRequirement.RequiredSetIdFieldNumber], requirement.RequiredSetId);
			if (requirement.CardRequirementType == CardRequirementType.NamedSet)
				return (DatabaseDefinition.ColumnName[CardRequirement.RequiredSetNameFieldNumber], requirement.RequiredSetName);
			if (requirement.CardRequirementType == CardRequirementType.Unset)
				throw new Exception($"Client must set CardRequirementType");

			throw new Exception($"Didn't know how to handle {requirement}.");
		}

		public static async ValueTask<(string Name, int Value)> GetInsertCardTypeValue(CardRequirement requirement, DbConnector connector)
		{
			return await GetInsertCardTypeValue(requirement.CardSetType, connector);
		}

		public static async ValueTask<(string Name, int Value)> GetInsertCardTypeValue(CardSetType type, DbConnector connector)
		{
			var cardSetId = await connector.Command($"select CardSetTypeId from cardsettypes where Name = '{s_cardSetTypeNameMap[type]}';").QuerySingleAsync<int>();

			return (DatabaseDefinition.ColumnName[CardRequirement.CardSetTypeFieldNumber], cardSetId);
		}

		private static Dictionary<int, CardSetType> s_cardSetTypeIdMap = new Dictionary<int, CardSetType>
		{
			{ 1, CardSetType.CardSetAdversary },
			{ 2, CardSetType.CardSetAlly },
			{ 3, CardSetType.CardSetMastermind },
			{ 4, CardSetType.CardSetNeutral },
			{ 5, CardSetType.CardSetBystander },
			{ 6, CardSetType.CardSetHenchman },
			{ 7, CardSetType.CardSetScheme },
		};

		private static Dictionary<CardSetType, string> s_cardSetTypeNameMap = new Dictionary<CardSetType, string>
		{
			{ CardSetType.CardSetAdversary, "Adversary" },
			{ CardSetType.CardSetAlly, "Ally" },
			{ CardSetType.CardSetBystander, "Bystander" },
			{ CardSetType.CardSetHenchman, "Henchman" },
			{ CardSetType.CardSetMastermind, "Mastermind" },
			{ CardSetType.CardSetNeutral, "Neutral" },
			{ CardSetType.CardSetScheme, "Scheme" },
		};

		public static IDatabaseDefinition<int> DatabaseDefinition = new CardRequirementDatabaseDefinition();
	}
}