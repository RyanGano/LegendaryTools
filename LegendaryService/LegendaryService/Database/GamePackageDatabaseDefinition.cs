using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LegendaryService.Database
{
	public class GamePackageDatabaseDefinition : IDatabaseDefinition<GamePackageField>
	{
		string IDatabaseDefinition<GamePackageField>.DefaultTableName => TableNames.GamePackages;
		IReadOnlyList<GamePackageField> IDatabaseDefinition<GamePackageField>.BasicFields { get => BasicGamePackageFields; }
		IReadOnlyDictionary<GamePackageField, string> IDatabaseDefinition<GamePackageField>.TableName { get => GamePackageSqlTableMap; }
		IReadOnlyDictionary<GamePackageField, string> IDatabaseDefinition<GamePackageField>.ColumnName { get => GamePackageSqlColumnMap; }
		IReadOnlyDictionary<GamePackageField, string> IDatabaseDefinition<GamePackageField>.JoinStatement { get => GamePackageSqlJoinMap; }

		static readonly IReadOnlyList<GamePackageField> BasicGamePackageFields = new GamePackageField[]
		{
			GamePackageField.Id,
			GamePackageField.Name,
			GamePackageField.PackageType,
			GamePackageField.BaseMap,
			GamePackageField.CoverImage
		};

		static readonly Dictionary<GamePackageField, string> GamePackageSqlColumnMap = new Dictionary<GamePackageField, string>
		{
			{ GamePackageField.Id, "GamePackageId" },
			{ GamePackageField.Name, "Name" },
			{ GamePackageField.CoverImage, "CoverImage" },
			{ GamePackageField.PackageType, "Name" },
			{ GamePackageField.BaseMap, "Name" }
		};

		static readonly Dictionary<GamePackageField, string> GamePackageSqlTableMap = new Dictionary<GamePackageField, string>
		{
			{ GamePackageField.Id, TableNames.GamePackages },
			{ GamePackageField.Name, TableNames.GamePackages },
			{ GamePackageField.CoverImage, TableNames.GamePackages },
			{ GamePackageField.PackageType, TableNames.PackageTypes },
			{ GamePackageField.BaseMap, TableNames.BaseMaps }
		};

		static readonly Dictionary<GamePackageField, string> GamePackageSqlJoinMap = new Dictionary<GamePackageField, string>
		{
			{ GamePackageField.PackageType, $"inner join {TableNames.PackageTypes} on {TableNames.PackageTypes}.PackageTypeId = {TableNames.GamePackages}.PackageTypeId" },
			{ GamePackageField.BaseMap, $"inner join {TableNames.BaseMaps} on {TableNames.BaseMaps}.BaseMapId = {TableNames.GamePackages}.BaseMapId" }
		};
	}
}
