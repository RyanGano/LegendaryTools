using Faithlife.Utility;
using LegendaryService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static LegendaryService.GameService;

namespace LegendaryClientConsole.Utility
{
	public static class ClassUtility
	{
		public static async Task DisplayClassesAsync(GameServiceClient client, string[] args)
		{
			if (args.FirstOrDefault() == null)
				await DisplayClassesAsync(client);
			else if (int.TryParse(args.FirstOrDefault(), out int id))
				await DisplayClassesAsync(client, classIds: args.Select(x => int.Parse(x)).ToList());
			else
				await DisplayClassesAsync(client, name: args.FirstOrDefault(), nameMatchStyle: NameMatchStyle.Similar);
		}

		public static async Task DisplayClassesAsync(GameServiceClient client, IReadOnlyList<int> classIds = null, string name = null, NameMatchStyle nameMatchStyle = NameMatchStyle.MixedCase)
		{
			var request = new GetClassesRequest();

			if (classIds != null && classIds.Count() != 0)
				request.ClassIds.AddRange(classIds);
			else if (!string.IsNullOrWhiteSpace(name))
				request.Name = name;

			request.NameMatchStyle = nameMatchStyle;

			var classes = await client.GetClassesAsync(request);

			foreach (var @class in classes.Classes)
				ConsoleUtility.WriteLine($"{@class}");
		}

		internal static async ValueTask<IEnumerable<ClassInfo>> SelectClassIds(GameServiceClient client)
		{
			List<ClassInfo> classInfos = new List<ClassInfo>();
			IReadOnlyList<Class> classes = null;

			while (true)
			{
				int classId = 0;

				int classCount = classInfos.Sum(x => x.Count);

				var input = ConsoleUtility.GetUserInput("What class is this entry associated with (? to see listing, empty to finish): ");
				
				if (input == "")
				{
					if (classCount < 14)
					{
						ConsoleUtility.WriteLine($"Must supply a class count of at least 14 (currently {classCount})");
					}
					else
					{
						if (ConsoleUtility.ShouldContinue($"Adding entry to classes [{classInfos.Select(x => x.ToString()).Join(", ")}] (Total {classCount}):"))
							return classInfos;

						return await SelectClassIds(client);
					}
				}

				if (input == "?")
				{
					classes = await DisplayClassesAsync(client, classes);
				}
				else if (!string.IsNullOrWhiteSpace(input))
				{
					classes = await GetClassesAsync(client, classes);

					if (int.TryParse(input, out int id))
					{
						classId = classes.Select(x => x.Id).FirstOrDefault(x => x == id, 0);
						if (classId == 0)
							ConsoleUtility.WriteLine($"Class Id '{input}' was not found");
					}
					else
					{
						var matchingClasses = classes.Where(x => Regex.IsMatch(x.Name.ToLower(), input.ToLower())).ToList();
						if (matchingClasses.Count == 0)
							ConsoleUtility.WriteLine($"Class Name '{input}' was not found");
						else if (matchingClasses.Count != 1)
							ConsoleUtility.WriteLine($"Class Name '{input}' matched multiple Classes ({matchingClasses.Select(x => x.Name).Join(", ")})");
						else
							classId = matchingClasses.First().Id;
					}
				}

				if (classId != 0)
				{
					var @class = classes.First(x => x.Id == classId);
					var cardCount = ConsoleUtility.GetUserInputInt("Enter number of cards for this class: ");
				
					var classInfo = new ClassInfo { ClassId = @class.Id, Count = cardCount };

					if (ConsoleUtility.ShouldContinue($"Adding entry to clases '{@class.Id}: {@class.Name}', count '{classInfo.Count}':"))
						classInfos.Add(classInfo);
				}
			}
		}

		public static async ValueTask<IReadOnlyList<Class>> DisplayClassesAsync(GameServiceClient client, IReadOnlyList<Class> classes)
		{
			classes = await GetClassesAsync(client, classes);

			foreach (var ability in classes)
				ConsoleUtility.WriteLine($"{ability.Id}: {ability.Name}");

			return classes;
		}

		public static async Task<IReadOnlyList<Class>> GetClassesAsync(GameServiceClient client, IReadOnlyList<Class> classes)
		{
			if (classes == null)
			{
				var request = new GetClassesRequest();
				request.Fields.AddRange(new[] { ClassField.ClassId, ClassField.ClassName });
				classes = (await client.GetClassesAsync(request)).Classes.ToList();
			}

			return classes;
		}
	}
}
