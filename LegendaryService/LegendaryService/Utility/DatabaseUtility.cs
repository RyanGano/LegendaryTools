using LegendaryService.Database;

namespace LegendaryService.Utility
{
	public static class DatabaseUtility
	{
		public static WhereStatementType GetWhereComparisonType(NameMatchStyle matchStyle)
		{
			return matchStyle == NameMatchStyle.Similar ?
				WhereStatementType.Like :
				matchStyle == NameMatchStyle.MixedCase ?
					WhereStatementType.Equals :
					WhereStatementType.BinaryEquals;
		}
	}
}
