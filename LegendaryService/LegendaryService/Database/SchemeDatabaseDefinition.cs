using System.Collections.Generic;

namespace LegendaryService.Database
{
	public class SchemeDatabaseDefinition : IDatabaseDefinition<SchemeField>
	{
		string IDatabaseDefinition<SchemeField>.DefaultTableName => TableNames.Schemes;
		IReadOnlyList<SchemeField> IDatabaseDefinition<SchemeField>.BasicFields { get => BasicSchemeFields; }
		IReadOnlyDictionary<SchemeField, string> IDatabaseDefinition<SchemeField>.ColumnName { get => SchemeSqlColumnMap; }
		IReadOnlyDictionary<SchemeField, string> IDatabaseDefinition<SchemeField>.TableName { get => SchemeSqlTableMap; }
		public IReadOnlyDictionary<SchemeField, string> JoinStatement { get => SchemeSqlJoinMap; }

		static readonly IReadOnlyList<SchemeField> BasicSchemeFields = new SchemeField[]
		{
			SchemeField.SchemeId,
			SchemeField.SchemeName,
			SchemeField.SchemeAbilityIds,
			SchemeField.SchemeEpicInformation,
			SchemeField.SchemeCardRequirements,
			SchemeField.SchemeGamePackageId
		};

		static readonly Dictionary<SchemeField, string> SchemeSqlColumnMap = new Dictionary<SchemeField, string>
		{
			{ SchemeField.SchemeId, "SchemeId" },
			{ SchemeField.SchemeName, "Name" },
			{ SchemeField.SchemeEpicInformation, "HasEpicSide" },
			{ SchemeField.SchemeGamePackageId, "GamePackageId" },
		};

		static readonly Dictionary<SchemeField, string> SchemeSqlTableMap = new Dictionary<SchemeField, string>
		{
			{ SchemeField.SchemeId, TableNames.Schemes },
			{ SchemeField.SchemeName, TableNames.Schemes },
			{ SchemeField.SchemeEpicInformation, TableNames.Schemes },
			{ SchemeField.SchemeGamePackageId, TableNames.GamePackages },
		};

		static readonly Dictionary<SchemeField, string> SchemeSqlJoinMap = new Dictionary<SchemeField, string>
		{
			{ SchemeField.SchemeGamePackageId, $@"
					inner join {TableNames.GamePackageSchemes} on {TableNames.GamePackageSchemes}.SchemeId = {TableNames.Schemes}.SchemeId
					inner join {TableNames.GamePackages} on {TableNames.GamePackages}.GamePackageId = {TableNames.GamePackageSchemes}.GamePackageId" },					
			{ SchemeField.SchemeCardRequirements, $@"
					inner join {TableNames.MatchedCardRequirements} on {TableNames.MatchedCardRequirements}.CardRequirementId = {TableNames.CardRequirements}.CardRequirementId
					inner join {TableNames.Schemes} on {TableNames.Schemes}.SchemeId = {TableNames.MatchedCardRequirements}.OwnerId" }
		};
	}
}
