using Faithlife.Utility;
using System;
using System.Linq;
using System.Text;

namespace LegendaryClientConsole.Utility
{
	public static class ConsoleUtility
	{
		public static string GetUserInput(string message)
		{
			Console.Write(message);
			return Console.ReadLine();
		}

		public static void WriteLine(string message)
		{
			Console.WriteLine(message);
		}

		public static bool ShouldContinue(string message)
		{
			Console.WriteLine(message);
			return GetUserInputBool("Continue?", true);
		}

		internal static int GetUserInputInt(string message)
		{
			string input = "";
			int result;

			while (!int.TryParse(input, out result))
				input = GetUserInput(message);

			return result;
		}

		internal static bool GetUserInputBool(string message, bool? defaultOption = null)
		{
			string hint = !defaultOption.HasValue ? "(y/n)" : defaultOption.Value ? "(Y/n)" : "(y/N)";
			var input = GetUserInput($"{message} {hint}: ");

			bool? enteredValue =
				input.StartsWith("y", StringComparison.OrdinalIgnoreCase) ?
					true :
					input.StartsWith("n", StringComparison.OrdinalIgnoreCase) ?
						false :
						(bool?)null;

			if (enteredValue.HasValue)
				return enteredValue.Value;

			if (defaultOption.HasValue && string.IsNullOrWhiteSpace(input))
				return defaultOption.Value;

			return GetUserInputBool(message, defaultOption);
		}

		internal static string GetUserInputRequiredValue(string message, params string[] options)
		{
			string input = null;

			var messageWithOptions = $"{message} ({options.Join("|")}): ";

			while (string.IsNullOrWhiteSpace(input) || options.Count(x => x.StartsWith(input, StringComparison.OrdinalIgnoreCase)) != 1)
				input = GetUserInput(messageWithOptions);

			return options.First(x => x.StartsWith(input, StringComparison.OrdinalIgnoreCase));
		}
	}
}
