using Faithlife.Data;
using LegendaryService.Database;
using LegendaryService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LegendaryService.Utility
{
	public static class SchemeTwistRequirementUtility
	{
		internal static async ValueTask<int> AddSchemeTwistRequirementAsync(SchemeTwistRequirement requirement)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			return (int)(await connector.Command(
				$@"
					insert into {TableNames.TwistRequirements} (TwistCount, IsAllowed)
						values ({requirement.SchemeTwistCount}, {requirement.Allowed});
					select last_insert_id();")
				.QuerySingleAsync<ulong>());
		}

		internal static async ValueTask<IReadOnlyList<SchemeTwistRequirement>> GetSchemeTwistRequirementsAsync(int schemeId)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var fields = DatabaseDefinition.BasicFields;

			var numberOfPlayersResult = $"{TableNames.SchemeTwistRequirements}_NumberOfPlayers";

			var selectStatement = DatabaseDefinition.BuildSelectStatement(fields);
			var joinStatement = DatabaseDefinition.BuildRequiredJoins(fields);
			var whereStatement = $"where {DatabaseDefinition.BuildWhereStatement(SchemeTwistRequirement.SchemeIdFieldNumber, WhereStatementType.Equals)}";

			// Create new card requirement
			return (await connector.Command($@"
				select {selectStatement}
					from {TableNames.TwistRequirements}
					{joinStatement}
					{whereStatement};",
					(DatabaseDefinition.GetSelectResult(SchemeTwistRequirement.SchemeIdFieldNumber), schemeId))
				.QueryAsync(x => MapSchemeTwistRequirement(x, DatabaseDefinition.BasicFields)));
		}

		private static SchemeTwistRequirement MapSchemeTwistRequirement(IDataRecord data, IReadOnlyList<int> fields)
		{
			var schemeTwistRequirement = new SchemeTwistRequirement();

			if (fields.Count == 0)
				fields = DatabaseDefinition.BasicFields;

			if (fields.Contains(SchemeTwistRequirement.IdFieldNumber))
				schemeTwistRequirement.Id = data.Get<int>(DatabaseDefinition.GetSelectResult(SchemeTwistRequirement.IdFieldNumber));
			if (fields.Contains(SchemeTwistRequirement.SchemeIdFieldNumber))
				schemeTwistRequirement.SchemeId = data.Get<int>(DatabaseDefinition.GetSelectResult(SchemeTwistRequirement.SchemeIdFieldNumber));
			if (fields.Contains(SchemeTwistRequirement.PlayerCountFieldNumber))
				schemeTwistRequirement.PlayerCount = data.Get<int>(DatabaseDefinition.GetSelectResult(SchemeTwistRequirement.PlayerCountFieldNumber));
			if (fields.Contains(SchemeTwistRequirement.SchemeTwistCountFieldNumber))
				schemeTwistRequirement.SchemeTwistCount = data.Get<int>(DatabaseDefinition.GetSelectResult(SchemeTwistRequirement.SchemeTwistCountFieldNumber));
			if (fields.Contains(SchemeTwistRequirement.AllowedFieldNumber))
				schemeTwistRequirement.Allowed = data.Get<bool>(DatabaseDefinition.GetSelectResult(SchemeTwistRequirement.AllowedFieldNumber));

			return schemeTwistRequirement;
		}

		public static IDatabaseDefinition<int> DatabaseDefinition = new SchemeTwistRequirementDatabaseDefinition();
	}
}