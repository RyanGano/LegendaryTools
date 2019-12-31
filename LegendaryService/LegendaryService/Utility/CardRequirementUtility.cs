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
		internal static async ValueTask<int> AddCardRequirement(CardRequirement requirement, string tableName, int sourceId)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			// Create new card requirement
			var insertColumnInfo = GetInsertColumnValue(requirement);
			var insertCardTypeInfo = await GetInsertCardTypeValue(requirement, connector);
			var query = $@"insert into cardrequirements ({insertColumnInfo.Name}, {insertCardTypeInfo.Name}) values (@{insertColumnInfo.Name}, @{insertCardTypeInfo.Name}); select last_insert_id();";
			
			return (int)await connector.Command(query,
				insertColumnInfo,
				insertCardTypeInfo)
				.QuerySingleAsync<ulong>();
		}

		internal static async ValueTask<IReadOnlyList<CardRequirement>> GetCardRequirementsAsync(Mastermind mastermind)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var fields = DatabaseDefinition.BasicFields;

			var numberOfPlayersResult = $"{TableNames.MastermindCardRequirements}_NumberOfPlayers";

			var selectStatement = DatabaseDefinition.BuildSelectStatement(fields.Except(new[] { CardRequirement.PlayerCountFieldNumber }).ToList()) + ", " + $"{TableNames.MastermindCardRequirements}.NumberOfPlayers as {numberOfPlayersResult}";
			var joinStatement = MastermindUtility.DatabaseDefinition.BuildRequiredJoins(new[] { MastermindField.MastermindCardRequirements }) + DatabaseDefinition.BuildRequiredJoins(new[] { CardRequirement.CardSetTypeFieldNumber });
			var whereStatement = $"where {MastermindUtility.DatabaseDefinition.BuildWhereStatement(MastermindField.MastermindId, WhereStatementType.Equals)}";

			// Create new card requirement
			return (await connector.Command($@"
				select {selectStatement}
					from {DatabaseDefinition.DefaultTableName}
					{joinStatement}
					{whereStatement};",
					(MastermindUtility.DatabaseDefinition.GetSelectResult(MastermindField.MastermindId), mastermind.Id))
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
				cardRequirement.CardSetType = MapToCardSetType(data.Get<string>(DatabaseDefinition.GetSelectResult(CardRequirement.CardSetTypeFieldNumber)));
			if (fields.Contains(CardRequirement.PlayerCountFieldNumber))
				cardRequirement.PlayerCount = data.Get<int>($"{TableNames.MastermindCardRequirements}_NumberOfPlayers");

			return cardRequirement;
		}

		private static (string Name, object Value) GetInsertColumnValue(CardRequirement requirement)
		{
			if (requirement.AdditionalSetCount != 0)
				return (DatabaseDefinition.ColumnName[CardRequirement.AdditionalSetCountFieldNumber], requirement.AdditionalSetCount);
			if (requirement.RequiredSetId != 0)
				return (DatabaseDefinition.ColumnName[CardRequirement.RequiredSetIdFieldNumber], requirement.RequiredSetId);
			if (!string.IsNullOrWhiteSpace(requirement.RequiredSetName))
				return (DatabaseDefinition.ColumnName[CardRequirement.RequiredSetNameFieldNumber], requirement.RequiredSetName);

			throw new Exception($"Didn't know how to handle {requirement}.");
		}

		private static async ValueTask<(string Name, int Value)> GetInsertCardTypeValue(CardRequirement requirement, DbConnector connector)
		{
			var cardSetId = await connector.Command($"select CardSetTypeId from cardsettypes where Name = '{MapToCardSetTypeName(requirement.CardSetType)}';").QuerySingleAsync<int>();
			
			return (DatabaseDefinition.ColumnName[CardRequirement.CardSetTypeFieldNumber], cardSetId);
		}

		private static string MapToCardSetTypeName(CardSetType cardSetType)
		{
			return cardSetType switch
			{
				CardSetType.CardSetAdversary => "Adversary",
				CardSetType.CardSetAlly => "Ally",
				CardSetType.CardSetBystander => "Bystander",
				CardSetType.CardSetHenchman => "Henchman",
				CardSetType.CardSetMastermind => "Mastermind",
				CardSetType.CardSetNeutral => "Neutral",
				CardSetType.CardSetScheme => "Scheme",
				_ => throw new Exception($"Didn't know how to handle {cardSetType}")
			};
		}

		private static CardSetType MapToCardSetType(string cardSetTypeName)
		{
			return cardSetTypeName switch
			{
				"Adversary" => CardSetType.CardSetAdversary,
				"Ally" => CardSetType.CardSetAlly,
				"Bystander" => CardSetType.CardSetBystander,
				"Henchman" => CardSetType.CardSetHenchman,
				"Mastermind" => CardSetType.CardSetMastermind,
				"Neutral" => CardSetType.CardSetNeutral,
				"Scheme" => CardSetType.CardSetScheme,
				_ => throw new Exception($"Didn't know how to handle {cardSetTypeName}")
			};
		}

		public static IDatabaseDefinition<int> DatabaseDefinition = new CardRequirementDatabaseDefinition();
	}
}