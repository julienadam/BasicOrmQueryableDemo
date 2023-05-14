using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace BasicOrmQueryableDemo.Provider;

public class DbQueryProvider : QueryProvider
{
    private readonly DbConnection connection;

    public DbQueryProvider(DbConnection connection)
    {
        this.connection = connection;
    }

    public override string GetQueryText(Expression expression)
    {
        return this.Translate(expression);
    }

    public override object? Execute(Expression expression)
    {
        var cmd = this.connection.CreateCommand();
        cmd.CommandText = this.Translate(expression);
        DbDataReader reader = cmd.ExecuteReader();
        var elementType = TypeSystem.GetElementType(expression.Type);
        return Activator.CreateInstance(
            typeof(ObjectReader<>).MakeGenericType(elementType),
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new object[] { reader },
            null);
    }

    private string Translate(Expression expression) => new QueryTranslator().Translate(expression);
}