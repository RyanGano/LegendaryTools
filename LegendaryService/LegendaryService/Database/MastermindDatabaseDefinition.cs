using System.Collections.Generic;

namespace LegendaryService.Database
{
	public class MastermindDatabaseDefinition : IDatabaseDefinition<MastermindField>
	{
		string IDatabaseDefinition<MastermindField>.DefaultTableName => TableNames.Masterminds;
		IReadOnlyList<MastermindField> IDatabaseDefinition<MastermindField>.BasicFields { get => BasicMastermindFields; }
		IReadOnlyDictionary<MastermindField, string> IDatabaseDefinition<MastermindField>.ColumnName { get => MastermindSqlColumnMap; }
		IReadOnlyDictionary<MastermindField, string> IDatabaseDefinition<MastermindField>.TableName { get => MastermindSqlTableMap; }
		public IReadOnlyDictionary<MastermindField, string> JoinStatement { get => MastermindSqlJoinMap; }

		static readonly IReadOnlyList<MastermindField> BasicMastermindFields = new MastermindField[]
		{
			MastermindField.MastermindId,
			MastermindField.MastermindName,
			MastermindField.MastermindAbilityIds,
			MastermindField.MastermindEpicInformation,
			MastermindField.MastermindCardRequirements,
			MastermindField.MastermindGamePackageId
		};

		static readonly Dictionary<MastermindField, string> MastermindSqlColumnMap = new Dictionary<MastermindField, string>
		{
			{ MastermindField.MastermindId, "MastermindId" },
			{ MastermindField.MastermindName, "Name" },
			{ MastermindField.MastermindEpicInformation, "HasEpicSide" },
			{ MastermindField.MastermindGamePackageId, "GamePackageId" },
		};

		static readonly Dictionary<MastermindField, string> MastermindSqlTableMap = new Dictionary<MastermindField, string>
		{
			{ MastermindField.MastermindId, TableNames.Masterminds },
			{ MastermindField.MastermindName, TableNames.Masterminds },
			{ MastermindField.MastermindEpicInformation, TableNames.Masterminds },
			{ MastermindField.MastermindGamePackageId, TableNames.GamePackages },
		};

		static readonly Dictionary<MastermindField, string> MastermindSqlJoinMap = new Dictionary<MastermindField, string>
		{
			{ MastermindField.MastermindGamePackageId, $@"
					inner join {TableNames.GamePackageMasterminds} on {TableNames.GamePackageMasterminds}.MastermindId = {TableNames.Masterminds}.MastermindId
					inner join {TableNames.GamePackages} on {TableNames.GamePackages}.GamePackageId = {TableNames.GamePackageMasterminds}.GamePackageId" },					
			{ MastermindField.MastermindCardRequirements, $@"
					inner join {TableNames.MastermindCardRequirements} on {TableNames.MastermindCardRequirements}.CardRequirementId = {TableNames.CardRequirements}.CardRequirementId
					inner join {TableNames.Masterminds} on {TableNames.Masterminds}.MastermindId = {TableNames.MastermindCardRequirements}.MastermindId" }
		};
	}
}
