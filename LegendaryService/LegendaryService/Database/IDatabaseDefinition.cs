using Faithlife.Utility;
using System.Collections.Generic;
using System.Linq;

namespace LegendaryService.Database
{
	public interface IDatabaseDefinition<T>
	{
		public string GetSelectStatement(T field) => $"{TableName[field]}.{ColumnName[field]} as {GetSelectResult(field)}";
		public string GetSelectResult(T field) => $"{TableName[field]}_{ColumnName[field]}";
		public string GetWhereStatement(T field) => $"{TableName[field]}.{ColumnName[field]}";

		public string DefaultTableName { get; }
		public IReadOnlyList<T> BasicFields { get; }
		public IReadOnlyDictionary<T, string> TableName { get; }
		public IReadOnlyDictionary<T, string> ColumnName { get; }
		public IReadOnlyDictionary<T, string> JoinStatement { get; }

		public string BuildSelectStatement(IReadOnlyList<T> fields)
		{
			if ((fields?.Count ?? 0) == 0)
				fields = BasicFields;

			return fields.Select(x => GetSelectStatement(x)).Join(", ");
		}

		public string BuildRequiredJoins(IReadOnlyList<T> fields)
		{
			if ((fields?.Count ?? 0) == 0)
				fields = BasicFields;

			return fields.Select(x => JoinStatement?.GetValueOrDefault(x, () => null)).Distinct().WhereNotNull().Join(" ") ?? "";
		}

		public string BuildWhereStatement(T field)
		{
			return $"{GetWhereStatement(field)} = @{GetSelectResult(field)}";
		}
	}
}
