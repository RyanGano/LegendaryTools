using Faithlife.Utility;
using System.Collections.Generic;
using System.Linq;

namespace LegendaryService.Database
{
	public interface IDatabaseDefinition<T>
	{
		public IReadOnlyList<T> BasicFields { get; }

		public IReadOnlyDictionary<T, string> SqlTableMap { get; }
		public IReadOnlyDictionary<T, string> SqlColumnMap { get; }

		public string BuildSelectStatement(IReadOnlyList<T> fields)
		{
			if ((fields?.Count ?? 0) == 0)
				fields = BasicFields;

			return fields.Select(x => MapFieldToSelectStatement(x)).Join(", ");
		}

		public string MapFieldToSelectStatement(T field) => $"{SqlTableMap[field]}.{SqlColumnMap[field]} as {MapTableFieldToSelectResult(field)}";
		public string MapTableFieldToSelectResult(T field) => $"{SqlTableMap[field]}_{SqlColumnMap[field]}";
	}
}
