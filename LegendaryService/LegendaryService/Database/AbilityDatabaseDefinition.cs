using System.Collections.Generic;

namespace LegendaryService.Database
{
	public class AbilityDatabaseDefinition : IDatabaseDefinition
	{
		IReadOnlyList<object> IDatabaseDefinition.BasicFields { get => BasicAbilityFields; }
		IReadOnlyDictionary<object, string> IDatabaseDefinition.SqlTableMap { get => AbilitySqlTableMap; }
		IReadOnlyDictionary<object, string> IDatabaseDefinition.SqlColumnMap { get => AbilitySqlColumnMap; }

		static readonly IReadOnlyList<object> BasicAbilityFields = new object[]
		{
			AbilityField.Id,
			AbilityField.Name,
			AbilityField.Description,
			AbilityField.GamePackageId
		};

		static readonly Dictionary<object, string> AbilitySqlColumnMap = new Dictionary<object, string>
		{
			{ AbilityField.Id, "AbilityId" },
			{ AbilityField.Name, "Name" },
			{ AbilityField.Description, "Description" },
			{ AbilityField.GamePackageId, "GamePackageId" },
			{ AbilityField.GamePackageName, "Name" }
		};

		static readonly Dictionary<object, string> AbilitySqlTableMap = new Dictionary<object, string>
		{
			{ AbilityField.Id, "abilities" },
			{ AbilityField.Name, "abilities" },
			{ AbilityField.Description, "abilities" },
			{ AbilityField.GamePackageId, "abilities" },
			{ AbilityField.GamePackageName, "gamepackages" }
		};
	}
}
