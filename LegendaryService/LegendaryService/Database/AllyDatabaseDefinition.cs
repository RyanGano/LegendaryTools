using System.Collections.Generic;

namespace LegendaryService.Database
{
	public class AllyDatabaseDefinition : IDatabaseDefinition<AllyField>
	{
		string IDatabaseDefinition<AllyField>.DefaultTableName => TableNames.Allies;
		IReadOnlyList<AllyField> IDatabaseDefinition<AllyField>.BasicFields { get => BasicAllyFields; }
		IReadOnlyDictionary<AllyField, string> IDatabaseDefinition<AllyField>.ColumnName { get => AllySqlColumnMap; }
		IReadOnlyDictionary<AllyField, string> IDatabaseDefinition<AllyField>.TableName { get => AllySqlTableMap; }
		public IReadOnlyDictionary<AllyField, string> JoinStatement { get => AllySqlJoinMap; }

		static readonly IReadOnlyList<AllyField> BasicAllyFields = new AllyField[]
		{
			AllyField.AllyId,
			AllyField.AllyName,
			AllyField.AllyAbilityIds,
			AllyField.AllyTeamId,
			AllyField.AllyGamePackageId,
		};

		static readonly Dictionary<AllyField, string> AllySqlColumnMap = new Dictionary<AllyField, string>
		{
			{ AllyField.AllyId, "AllyId" },
			{ AllyField.AllyName, "Name" },
			{ AllyField.AllyTeamId, "TeamId" },
			{ AllyField.AllyGamePackageId, "GamePackageId" },
		};

		static readonly Dictionary<AllyField, string> AllySqlTableMap = new Dictionary<AllyField, string>
		{
			{ AllyField.AllyId, TableNames.Allies },
			{ AllyField.AllyName, TableNames.Allies },
			{ AllyField.AllyTeamId, TableNames.Allies },
			{ AllyField.AllyGamePackageId, TableNames.GamePackages },
		};

		static readonly Dictionary<AllyField, string> AllySqlJoinMap = new Dictionary<AllyField, string>
		{
			{ AllyField.AllyGamePackageId, $@"
					inner join {TableNames.GamePackageAllies} on {TableNames.GamePackageAllies}.AllyId = {TableNames.Allies}.AllyId
					inner join {TableNames.GamePackages} on {TableNames.GamePackages}.GamePackageId = {TableNames.GamePackageAllies}.GamePackageId" }
		};
	}
}
