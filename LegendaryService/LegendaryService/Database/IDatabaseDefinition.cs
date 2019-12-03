using Faithlife.Utility;
using System.Collections.Generic;
using System.Linq;

namespace LegendaryService.Database
{
	public interface IDatabaseDefinition
	{
		public IReadOnlyList<object> BasicFields { get; }

		public IReadOnlyDictionary<object, string> SqlTableMap { get; }
		public IReadOnlyDictionary<object, string> SqlColumnMap { get; }

		public string BuildSelectStatement(IReadOnlyList<object> fields)
		{
			if ((fields?.Count ?? 0) == 0)
				fields = BasicFields;

			return fields.Select(x => MapFieldToSelectStatement(x)).Join(", ");
		}

		public string MapFieldToSelectStatement(object field) => $"{SqlTableMap[field]}.{SqlColumnMap[field]} as {MapTableFieldToSelectResult(field)}";
		public string MapTableFieldToSelectResult(object field) => $"{SqlTableMap[field]}_{SqlColumnMap[field]}";
	}
}
