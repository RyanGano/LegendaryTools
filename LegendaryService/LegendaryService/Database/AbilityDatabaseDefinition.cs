using System.Collections.Generic;

namespace LegendaryService.Database
{
	public class AbilityDatabaseDefinition : IDatabaseDefinition<AbilityField>
	{
		string IDatabaseDefinition<AbilityField>.DefaultTableName => TableNames.Abilities;
		IReadOnlyList<AbilityField> IDatabaseDefinition<AbilityField>.BasicFields { get => BasicAbilityFields; }
		IReadOnlyDictionary<AbilityField, string> IDatabaseDefinition<AbilityField>.TableName { get => AbilitySqlTableMap; }
		IReadOnlyDictionary<AbilityField, string> IDatabaseDefinition<AbilityField>.ColumnName { get => AbilitySqlColumnMap; }
		IReadOnlyDictionary<AbilityField, string> IDatabaseDefinition<AbilityField>.JoinStatement { get => AbilitySqlJoinMap; }

		static readonly IReadOnlyList<AbilityField> BasicAbilityFields = new AbilityField[]
		{
			AbilityField.Id,
			AbilityField.Name,
			AbilityField.Description,
			AbilityField.GamePackageId
		};

		static readonly Dictionary<AbilityField, string> AbilitySqlColumnMap = new Dictionary<AbilityField, string>
		{
			{ AbilityField.Id, "AbilityId" },
			{ AbilityField.Name, "Name" },
			{ AbilityField.Description, "Description" },
			{ AbilityField.GamePackageId, "GamePackageId" },
			{ AbilityField.GamePackageName, "Name" }
		};

		static readonly Dictionary<AbilityField, string> AbilitySqlTableMap = new Dictionary<AbilityField, string>
		{
			{ AbilityField.Id, TableNames.Abilities },
			{ AbilityField.Name, TableNames.Abilities },
			{ AbilityField.Description, TableNames.Abilities },
			{ AbilityField.GamePackageId, TableNames.Abilities },
			{ AbilityField.GamePackageName, TableNames.GamePackages }
		};

		static readonly Dictionary<AbilityField, string> AbilitySqlJoinMap = new Dictionary<AbilityField, string>
		{
			{ AbilityField.GamePackageName, $"inner join {TableNames.GamePackages} on {TableNames.GamePackages}.{AbilitySqlColumnMap[AbilityField.GamePackageId]} = {TableNames.Abilities}.{AbilitySqlColumnMap[AbilityField.GamePackageId]}" }
		};
	}
}
