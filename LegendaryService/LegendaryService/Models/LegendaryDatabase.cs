using Faithlife.Data;
using Faithlife.Utility;
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

			foreach (var match in whereMatch)
			{
				if (match.Item2 is int[])
				{
					string items = ((int[])match.Item2).Select(x => x.ToString()).Join(", ");
					query = query.Replace($"@{match.Item1}", $"({items})");
				}
				if (match.Item2 is string) 
				{
					query = query.Replace($"@{match.Item1}", $"\"{match.Item2.ToString()}\"");
				}
				else
				{
					query = query.Replace($"@{match.Item1}", $"({match.Item2.ToString()})");
				}
			}
			
			return await connector.Command(query).QueryAsync(x => handleResult(x));
			
			throw new ArgumentException($"Can't handle {whereMatch.Count()} match count yet.");
		}
	}
}
