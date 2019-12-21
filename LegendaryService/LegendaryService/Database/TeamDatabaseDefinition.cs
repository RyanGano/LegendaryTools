using System.Collections.Generic;

namespace LegendaryService.Database
{
	public class TeamDatabaseDefinition : IDatabaseDefinition<TeamField>
	{
		string IDatabaseDefinition<TeamField>.DefaultTableName => TableNames.Teams;
		IReadOnlyList<TeamField> IDatabaseDefinition<TeamField>.BasicFields { get => BasicTeamFields; }
		IReadOnlyDictionary<TeamField, string> IDatabaseDefinition<TeamField>.TableName { get => TeamSqlTableMap; }
		IReadOnlyDictionary<TeamField, string> IDatabaseDefinition<TeamField>.ColumnName { get => TeamSqlColumnMap; }
		public IReadOnlyDictionary<TeamField, string> JoinStatement { get => null; }

		static readonly IReadOnlyList<TeamField> BasicTeamFields = new TeamField[]
		{
			TeamField.TeamId,
			TeamField.TeamName,
			TeamField.TeamImagePath,
		};

		static readonly Dictionary<TeamField, string> TeamSqlColumnMap = new Dictionary<TeamField, string>
		{
			{ TeamField.TeamId, "TeamId" },
			{ TeamField.TeamName, "Name" },
			{ TeamField.TeamImagePath, "ImagePath" }
		};

		static readonly Dictionary<TeamField, string> TeamSqlTableMap = new Dictionary<TeamField, string>
		{
			{ TeamField.TeamId, TableNames.Teams },
			{ TeamField.TeamName, TableNames.Teams },
			{ TeamField.TeamImagePath, TableNames.Teams }
		};
	}
}
