using System.Collections.Generic;

namespace LegendaryService.Database
{
	public class SchemeTwistRequirementDatabaseDefinition : IDatabaseDefinition<int>
	{
		string IDatabaseDefinition<int>.DefaultTableName => TableNames.SchemeTwistRequirements;
		IReadOnlyList<int> IDatabaseDefinition<int>.BasicFields { get => BasicSchemeTwistRequirementFields; }
		IReadOnlyDictionary<int, string> IDatabaseDefinition<int>.ColumnName { get => SchemeTwistRequirementSqlColumnMap; }
		IReadOnlyDictionary<int, string> IDatabaseDefinition<int>.TableName { get => SchemeTwistRequirementSqlTableMap; }
		public IReadOnlyDictionary<int, string> JoinStatement { get => SchemeTwistRequirementSqlJoinMap; }

		static readonly IReadOnlyList<int> BasicSchemeTwistRequirementFields = new int[]
		{
			SchemeTwistRequirement.IdFieldNumber,
			SchemeTwistRequirement.SchemeTwistCountFieldNumber,
			SchemeTwistRequirement.PlayerCountFieldNumber,
			SchemeTwistRequirement.AllowedFieldNumber,
			SchemeTwistRequirement.SchemeIdFieldNumber
		};

		static readonly Dictionary<int, string> SchemeTwistRequirementSqlColumnMap = new Dictionary<int, string>
		{
			{ SchemeTwistRequirement.IdFieldNumber, "TwistRequirementId" },
			{ SchemeTwistRequirement.SchemeTwistCountFieldNumber, "TwistCount" },
			{ SchemeTwistRequirement.PlayerCountFieldNumber, "NumberOfPlayers" },
			{ SchemeTwistRequirement.AllowedFieldNumber, "IsAllowed" },
			{ SchemeTwistRequirement.SchemeIdFieldNumber, "SchemeId" },
		};

		static readonly Dictionary<int, string> SchemeTwistRequirementSqlTableMap = new Dictionary<int, string>
		{
			{ SchemeTwistRequirement.IdFieldNumber, TableNames.TwistRequirements },
			{ SchemeTwistRequirement.SchemeTwistCountFieldNumber, TableNames.TwistRequirements },
			{ SchemeTwistRequirement.PlayerCountFieldNumber, TableNames.SchemeTwistRequirements },
			{ SchemeTwistRequirement.AllowedFieldNumber, TableNames.TwistRequirements },
			{ SchemeTwistRequirement.SchemeIdFieldNumber, TableNames.SchemeTwistRequirements },
		};

		static readonly Dictionary<int, string> SchemeTwistRequirementSqlJoinMap = new Dictionary<int, string>
		{
			{ SchemeTwistRequirement.PlayerCountFieldNumber, $@"
				inner join {TableNames.SchemeTwistRequirements} on {TableNames.SchemeTwistRequirements}.TwistRequirementId = {TableNames.TwistRequirements}.TwistRequirementId" },
			{ SchemeTwistRequirement.SchemeIdFieldNumber, $@"
				inner join {TableNames.SchemeTwistRequirements} on {TableNames.SchemeTwistRequirements}.TwistRequirementId = {TableNames.TwistRequirements}.TwistRequirementId" },
		};
	}
}
