using System.Collections.Generic;

namespace LegendaryService.Database
{
	public class ClassDatabaseDefinition : IDatabaseDefinition<ClassField>
	{
		string IDatabaseDefinition<ClassField>.DefaultTableName => TableNames.Classes;
		IReadOnlyList<ClassField> IDatabaseDefinition<ClassField>.BasicFields { get => BasicClassFields; }
		IReadOnlyDictionary<ClassField, string> IDatabaseDefinition<ClassField>.TableName { get => ClassSqlTableMap; }
		IReadOnlyDictionary<ClassField, string> IDatabaseDefinition<ClassField>.ColumnName { get => ClassSqlColumnMap; }
		public IReadOnlyDictionary<ClassField, string> JoinStatement { get => null; }

		static readonly IReadOnlyList<ClassField> BasicClassFields = new ClassField[]
		{
			ClassField.ClassId,
			ClassField.ClassName,
			ClassField.ClassImagePath,
		};

		static readonly Dictionary<ClassField, string> ClassSqlColumnMap = new Dictionary<ClassField, string>
		{
			{ ClassField.ClassId, "ClassId" },
			{ ClassField.ClassName, "Name" },
			{ ClassField.ClassImagePath, "ImagePath" }
		};

		static readonly Dictionary<ClassField, string> ClassSqlTableMap = new Dictionary<ClassField, string>
		{
			{ ClassField.ClassId, TableNames.Classes },
			{ ClassField.ClassName, TableNames.Classes },
			{ ClassField.ClassImagePath, TableNames.Classes }
		};
	}
}
