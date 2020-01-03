using System.Collections.Generic;

namespace LegendaryService.Database
{
	public class CardRequirementDatabaseDefinition : IDatabaseDefinition<int>
	{
		string IDatabaseDefinition<int>.DefaultTableName => TableNames.CardRequirements;
		IReadOnlyList<int> IDatabaseDefinition<int>.BasicFields { get => BasicCardRequirementFields; }
		IReadOnlyDictionary<int, string> IDatabaseDefinition<int>.ColumnName { get => CardRequirementSqlColumnMap; }
		IReadOnlyDictionary<int, string> IDatabaseDefinition<int>.TableName { get => CardRequirementSqlTableMap; }
		public IReadOnlyDictionary<int, string> JoinStatement { get => CardRequirementSqlJoinMap; }

		static readonly IReadOnlyList<int> BasicCardRequirementFields = new int[]
		{
			CardRequirement.CardRequirementIdFieldNumber,
			CardRequirement.RequiredSetIdFieldNumber,
			CardRequirement.AdditionalSetCountFieldNumber,
			CardRequirement.RequiredSetNameFieldNumber,
			CardRequirement.CardSetTypeFieldNumber,
			CardRequirement.PlayerCountFieldNumber,
			CardRequirement.OwnerIdFieldNumber
		};

		static readonly Dictionary<int, string> CardRequirementSqlColumnMap = new Dictionary<int, string>
		{
			{ CardRequirement.CardRequirementIdFieldNumber, "CardRequirementId" },
			{ CardRequirement.RequiredSetIdFieldNumber, "AdditionalCardSetId" },
			{ CardRequirement.AdditionalSetCountFieldNumber, "AdditionalCardSetCount" },
			{ CardRequirement.RequiredSetNameFieldNumber, "AdditionalCardSetName" },
			{ CardRequirement.CardSetTypeFieldNumber, "CardSetTypeId" },
			{ CardRequirement.PlayerCountFieldNumber, "NumberOfPlayers" },
			{ CardRequirement.OwnerIdFieldNumber, "OwnerId" }
		};

		static readonly Dictionary<int, string> CardRequirementSqlTableMap = new Dictionary<int, string>
		{
			{ CardRequirement.CardRequirementIdFieldNumber, TableNames.CardRequirements },
			{ CardRequirement.RequiredSetIdFieldNumber, TableNames.CardRequirements },
			{ CardRequirement.AdditionalSetCountFieldNumber, TableNames.CardRequirements },
			{ CardRequirement.RequiredSetNameFieldNumber, TableNames.CardRequirements },
			{ CardRequirement.CardSetTypeFieldNumber, TableNames.CardSetTypes },
			{ CardRequirement.PlayerCountFieldNumber, TableNames.MatchedCardRequirements },
			{ CardRequirement.OwnerIdFieldNumber, TableNames.MatchedCardRequirements }
		};

		static readonly Dictionary<int, string> CardRequirementSqlJoinMap = new Dictionary<int, string>
		{
			{ CardRequirement.CardSetTypeFieldNumber, $@"
				inner join {TableNames.CardSetTypes} on {TableNames.CardSetTypes}.CardSetTypeId = {TableNames.CardRequirements}.CardSetTypeId" },
			{ CardRequirement.PlayerCountFieldNumber, $@"
				inner join {TableNames.MatchedCardRequirements} on {TableNames.MatchedCardRequirements}.CardRequirementId = {TableNames.CardRequirements}.CardRequirementId" },
			{ CardRequirement.OwnerIdFieldNumber, $@"
				inner join {TableNames.MatchedCardRequirements} on {TableNames.MatchedCardRequirements}.CardRequirementId = {TableNames.CardRequirements}.CardRequirementId" }
		};
	}
}
