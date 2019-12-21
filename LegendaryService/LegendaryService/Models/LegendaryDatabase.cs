using Faithlife.Data;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LegendaryService.Models
{
	public class LegendaryDatabase : IDisposable
	{
		public readonly MySqlConnection Connection;
		public LegendaryDatabase()
		{
			var connectionString = File.ReadAllText("Database.connection");
			Connection = new MySqlConnection(connectionString);
		}

		public void Dispose()
		{
			Connection.Close();
		}

		internal static string Encode(string input)
		{
			return input.Replace("(", ".(").Replace(")", ".)");
		}

		internal async Task<IReadOnlyList<T>> RunCommand<T>(DbConnector connector, string query, (string, object)[] whereMatch, Func<IDataRecord, T> handleResult)
		{
			if (whereMatch.Count() == 0)
				return await connector.Command(query).QueryAsync(x => handleResult(x));

			if (whereMatch.Count() == 1)
			return await connector.Command(query, (whereMatch.First().Item1, whereMatch.First().Item2)).QueryAsync(x => handleResult(x));

			throw new ArgumentException($"Can't handle {whereMatch.Count()} match count yet.");
		}
	}
}
