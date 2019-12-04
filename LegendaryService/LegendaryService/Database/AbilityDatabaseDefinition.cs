using System.Collections.Generic;

namespace LegendaryService.Database
{
	public class AbilityDatabaseDefinition : IDatabaseDefinition<AbilityField>
	{
		IReadOnlyList<AbilityField> IDatabaseDefinition<AbilityField>.BasicFields { get => BasicAbilityFields; }
		IReadOnlyDictionary<AbilityField, string> IDatabaseDefinition<AbilityField>.SqlTableMap { get => AbilitySqlTableMap; }
		IReadOnlyDictionary<AbilityField, string> IDatabaseDefinition<AbilityField>.SqlColumnMap { get => AbilitySqlColumnMap; }

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
			{ AbilityField.Id, "abilities" },
			{ AbilityField.Name, "abilities" },
			{ AbilityField.Description, "abilities" },
			{ AbilityField.GamePackageId, "abilities" },
			{ AbilityField.GamePackageName, "gamepackages" }
		};
	}
}
