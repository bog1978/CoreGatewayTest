using LinqToDB;
using LinqToDB.Linq;

namespace CoreGateway.Dispatcher.DataAccess
{
    internal static class PgSql
    {
        [Sql.Extension("ARRAY_APPEND({array}, {element})", ServerSideOnly = true, CanBeNull = true, Precedence = 100)]
        public static T[] ArrayAppend<T>([ExprParameter] T[] array, [ExprParameter] T[] element)
        {
            throw new LinqException("'ArrayAppend' is server-side method.");
        }
    }
}
