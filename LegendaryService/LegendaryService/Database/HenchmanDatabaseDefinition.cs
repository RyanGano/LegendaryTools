using System.Collections.Generic;

namespace LegendaryService.Database
{
	public class HenchmanDatabaseDefinition : IDatabaseDefinition<HenchmanField>
	{
		string IDatabaseDefinition<HenchmanField>.DefaultTableName => TableNames.Henchmen;
		IReadOnlyList<HenchmanField> IDatabaseDefinition<HenchmanField>.BasicFields { get => BasicHenchmanFields; }
		IReadOnlyDictionary<HenchmanField, string> IDatabaseDefinition<HenchmanField>.ColumnName { get => HenchmanSqlColumnMap; }
		IReadOnlyDictionary<HenchmanField, string> IDatabaseDefinition<HenchmanField>.TableName { get => HenchmanSqlTableMap; }
		public IReadOnlyDictionary<HenchmanField, string> JoinStatement { get => HenchmanSqlJoinMap; }

		static readonly IReadOnlyList<HenchmanField> BasicHenchmanFields = new HenchmanField[]
		{
			HenchmanField.HenchmanId,
			HenchmanField.HenchmanName,
			HenchmanField.HenchmanAbilityIds,
			HenchmanField.HenchmanGamePackageId,
		};

		static readonly Dictionary<HenchmanField, string> HenchmanSqlColumnMap = new Dictionary<HenchmanField, string>
		{
			{ HenchmanField.HenchmanId, "HenchmanId" },
			{ HenchmanField.HenchmanName, "Name" },
			{ HenchmanField.HenchmanGamePackageId, "GamePackageId" },
		};

		static readonly Dictionary<HenchmanField, string> HenchmanSqlTableMap = new Dictionary<HenchmanField, string>
		{
			{ HenchmanField.HenchmanId, TableNames.Henchmen },
			{ HenchmanField.HenchmanName, TableNames.Henchmen },
			{ HenchmanField.HenchmanGamePackageId, TableNames.GamePackages },
		};

		static readonly Dictionary<HenchmanField, string> HenchmanSqlJoinMap = new Dictionary<HenchmanField, string>
		{
			{ HenchmanField.HenchmanGamePackageId, $@"
					inner join {TableNames.GamePackageHenchmen} on {TableNames.GamePackageHenchmen}.HenchmanId = {TableNames.Henchmen}.HenchmanId
					inner join {TableNames.GamePackages} on {TableNames.GamePackages}.GamePackageId = {TableNames.GamePackageHenchmen}.GamePackageId" }
		};
	}
}
