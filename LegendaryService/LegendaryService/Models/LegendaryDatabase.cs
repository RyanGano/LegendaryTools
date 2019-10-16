using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
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
	}
}
