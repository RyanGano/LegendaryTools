using Faithlife.Data;
using Faithlife.Utility;
using Grpc.Core;
using LegendaryService.Database;
using LegendaryService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LegendaryService.Utility
{
	public static class ClassUtility
	{
		public static async Task<GetClassesReply> GetClassesAsync(GetClassesRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new GetClassesReply { Status = new Status { Code = 200 } };

			var select = DatabaseDefinition.BuildSelectStatement(request.Fields);
			var joins = DatabaseDefinition.BuildRequiredJoins(request.Fields);

			var where = !string.IsNullOrWhiteSpace(request.Name) ?
					$"where { DatabaseDefinition.BuildWhereStatement(ClassField.ClassName, WhereStatementType.Like)}" :
					request.ClassIds.Count() != 0 ?
						$"where { DatabaseDefinition.BuildWhereStatement(ClassField.ClassId, WhereStatementType.Includes)}" :
						"";

			var whereMatch = !string.IsNullOrWhiteSpace(request.Name) ?
					new (string, object)[] { (DatabaseDefinition.GetSelectResult(ClassField.ClassName), $"%{request.Name}%") } :
					request.ClassIds.Count() != 0 ?
						new (string, object)[] { (DatabaseDefinition.GetSelectResult(ClassField.ClassId), request.ClassIds.ToArray()) } :
						new (string, object)[] { };

			reply.Classes.AddRange(await db.RunCommand(connector,
				$@"select {select} from {DatabaseDefinition.DefaultTableName} {joins} {where};",
				whereMatch,
				x => MapClass(x, request.Fields)));

			return reply;
		}

		public static async Task<CreateClassesReply> CreateClassesAsync(CreateClassesRequest request, ServerCallContext context)
		{
			using var db = new LegendaryDatabase();
			var connector = DbConnector.Create(db.Connection, new DbConnectorSettings { AutoOpen = true, LazyOpen = true });

			var reply = new CreateClassesReply { Status = new Status { Code = 200 } };

			var classes = await GetClassesAsync(new GetClassesRequest(), context);

			var classesToAdd = request.Classes.Where(x => !classes.Classes.Any(y => y.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase))).ToList();

			if (classesToAdd.Count != request.Classes.Count() && request.CreateOptions.Contains(CreateOptions.ErrorOnDuplicates))
			{
				reply.Status.Code = 400;
				reply.Status.Message = $"Cannot add duplicate classes ({request.Classes.Except(classesToAdd).Select(x => x.Name).Join(", ")}).";
				return reply;
			}

			List<int> insertIds = request.Classes.Except(classesToAdd).Select(x => x.Id).ToList();

			foreach (var @class in classesToAdd)
			{
				insertIds.Add((int)(await connector.Command($@"
					insert
						into {DatabaseDefinition.DefaultTableName}
							({DatabaseDefinition.ColumnName[ClassField.ClassName]}, {DatabaseDefinition.ColumnName[ClassField.ClassImagePath]})
						values (@ClassName, @ImagePath);
					select last_insert_id();",
					("ClassName", @class.Name),
					("ImagePath", @class.ImagePath))
					.QuerySingleAsync<ulong>()));
			}

			var finalClassesList = new GetClassesRequest();
			finalClassesList.ClassIds.AddRange(insertIds);
			var createdClasses = await GetClassesAsync(finalClassesList, context);

			reply.Classes.AddRange(createdClasses.Classes);
			return reply;
		}

		private static Class MapClass(IDataRecord data, IReadOnlyList<ClassField> fields)
		{
			var @class = new Class();

			if (fields.Count == 0)
				fields = DatabaseDefinition.BasicFields;

			if (fields.Contains(ClassField.ClassId))
				@class.Id = data.Get<int>(DatabaseDefinition.GetSelectResult(ClassField.ClassId));
			if (fields.Contains(ClassField.ClassName))
				@class.Name = data.Get<string>(DatabaseDefinition.GetSelectResult(ClassField.ClassName));
			if (fields.Contains(ClassField.ClassImagePath))
				@class.ImagePath = data.Get<string>(DatabaseDefinition.GetSelectResult(ClassField.ClassImagePath));

			return @class;
		}

		public static IDatabaseDefinition<ClassField> DatabaseDefinition = new ClassDatabaseDefinition();
	}
}
