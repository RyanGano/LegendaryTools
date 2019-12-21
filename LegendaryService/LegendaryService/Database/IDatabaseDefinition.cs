using Faithlife.Utility;
using System.Collections.Generic;
using System.Linq;

namespace LegendaryService.Database
{
	public interface IDatabaseDefinition<T>
	{
		public string GetSelectStatement(T field) => $"{TableName.GetValueOrDefault(field, () => "[Field not found]")}.{ColumnName[field]} as {GetSelectResult(field)}";
		public string GetSelectResult(T field) => $"{TableName.GetValueOrDefault(field, () => "[Field not found]")}_{ColumnName.GetValueOrDefault(field, () => "[Field not found]")}";
		public string GetWhereStatement(T field) => $"{TableName.GetValueOrDefault(field, () => "[Field not found]")}.{ColumnName.GetValueOrDefault(field, () => "[Field not found]")}";

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

		public string BuildWhereStatement(T field, WhereStatementType type)
		{
			if (type == WhereStatementType.NotEquals)
				return $"{GetWhereStatement(field)} != @{GetSelectResult(field)}";
			if (type == WhereStatementType.Equals)
				return $"{GetWhereStatement(field)} = @{GetSelectResult(field)}";
			else
				return $"{GetWhereStatement(field)} in @{GetSelectResult(field)}";
		}
	}
}
