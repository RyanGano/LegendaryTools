using System.Collections.Generic;

namespace LegendaryService.Database
{
	public class NeutralDatabaseDefinition : IDatabaseDefinition<NeutralField>
	{
		string IDatabaseDefinition<NeutralField>.DefaultTableName => TableNames.Neutrals;
		IReadOnlyList<NeutralField> IDatabaseDefinition<NeutralField>.BasicFields { get => BasicNeutralFields; }
		IReadOnlyDictionary<NeutralField, string> IDatabaseDefinition<NeutralField>.ColumnName { get => NeutralSqlColumnMap; }
		IReadOnlyDictionary<NeutralField, string> IDatabaseDefinition<NeutralField>.TableName { get => NeutralSqlTableMap; }
		public IReadOnlyDictionary<NeutralField, string> JoinStatement { get => NeutralSqlJoinMap; }

		static readonly IReadOnlyList<NeutralField> BasicNeutralFields = new NeutralField[]
		{
			NeutralField.NeutralId,
			NeutralField.NeutralName,
			NeutralField.NeutralGamePackageId,
		};

		static readonly Dictionary<NeutralField, string> NeutralSqlColumnMap = new Dictionary<NeutralField, string>
		{
			{ NeutralField.NeutralId, "NeutralId" },
			{ NeutralField.NeutralName, "Name" },
			{ NeutralField.NeutralGamePackageId, "GamePackageId" },
		};

		static readonly Dictionary<NeutralField, string> NeutralSqlTableMap = new Dictionary<NeutralField, string>
		{
			{ NeutralField.NeutralId, TableNames.Neutrals },
			{ NeutralField.NeutralName, TableNames.Neutrals },
			{ NeutralField.NeutralGamePackageId, TableNames.GamePackages },
		};

		static readonly Dictionary<NeutralField, string> NeutralSqlJoinMap = new Dictionary<NeutralField, string>
		{
			{ NeutralField.NeutralGamePackageId, $@"
					inner join {TableNames.GamePackageNeutrals} on {TableNames.GamePackageNeutrals}.NeutralId = {TableNames.Neutrals}.NeutralId
					inner join {TableNames.GamePackages} on {TableNames.GamePackages}.GamePackageId = {TableNames.GamePackageNeutrals}.GamePackageId" }
		};
	}
}
