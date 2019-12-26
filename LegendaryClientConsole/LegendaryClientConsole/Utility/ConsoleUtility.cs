using System;
using System.Collections.Generic;
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
			var input = GetUserInput($"Continue? (Y/n): ");
			return input == "" || input.ToLower() == "y";
		}
	}
}
