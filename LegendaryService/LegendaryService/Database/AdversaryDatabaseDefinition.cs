using System.Collections.Generic;

namespace LegendaryService.Database
{
	public class AdversaryDatabaseDefinition : IDatabaseDefinition<AdversaryField>
	{
		string IDatabaseDefinition<AdversaryField>.DefaultTableName => TableNames.Adversaries;
		IReadOnlyList<AdversaryField> IDatabaseDefinition<AdversaryField>.BasicFields { get => BasicAdversaryFields; }
		IReadOnlyDictionary<AdversaryField, string> IDatabaseDefinition<AdversaryField>.ColumnName { get => AdversarySqlColumnMap; }
		IReadOnlyDictionary<AdversaryField, string> IDatabaseDefinition<AdversaryField>.TableName { get => AdversarySqlTableMap; }
		public IReadOnlyDictionary<AdversaryField, string> JoinStatement { get => AdversarySqlJoinMap; }

		static readonly IReadOnlyList<AdversaryField> BasicAdversaryFields = new AdversaryField[]
		{
			AdversaryField.AdversaryId,
			AdversaryField.AdversaryName,
			AdversaryField.AdversaryAbilityIds,
			AdversaryField.AdversaryGamePackageId,
		};

		static readonly Dictionary<AdversaryField, string> AdversarySqlColumnMap = new Dictionary<AdversaryField, string>
		{
			{ AdversaryField.AdversaryId, "AdversaryId" },
			{ AdversaryField.AdversaryName, "Name" },
			{ AdversaryField.AdversaryGamePackageId, "GamePackageId" },
		};

		static readonly Dictionary<AdversaryField, string> AdversarySqlTableMap = new Dictionary<AdversaryField, string>
		{
			{ AdversaryField.AdversaryId, TableNames.Adversaries },
			{ AdversaryField.AdversaryName, TableNames.Adversaries },
			{ AdversaryField.AdversaryGamePackageId, TableNames.GamePackages },
		};

		static readonly Dictionary<AdversaryField, string> AdversarySqlJoinMap = new Dictionary<AdversaryField, string>
		{
			{ AdversaryField.AdversaryGamePackageId, $@"
					inner join {TableNames.GamePackageAdversaries} on {TableNames.GamePackageAdversaries}.AdversaryId = {TableNames.Adversaries}.AdversaryId
					inner join {TableNames.GamePackages} on {TableNames.GamePackages}.GamePackageId = {TableNames.GamePackageAdversaries}.GamePackageId" }
		};
	}
}
