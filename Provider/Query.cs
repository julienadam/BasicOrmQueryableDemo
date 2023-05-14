using System.Linq.Expressions;
using System.Collections;

public class Query<T> : IQueryable<T>, IQueryable, IEnumerable<T>, IEnumerable, IOrderedQueryable<T>, IOrderedQueryable
{
    private readonly QueryProvider provider;
    private readonly Expression expression;

    public Query(QueryProvider provider)
    {
        this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        this.expression = Expression.Constant(this);
    }

    public Query(QueryProvider provider, Expression expression)
    {
        this.provider = provider ?? throw new ArgumentNullException(nameof(provider));

        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
        {
            throw new ArgumentOutOfRangeException(nameof(expression));
        }

        this.expression = expression;
    }

    Expression IQueryable.Expression => this.expression;

    Type IQueryable.ElementType => typeof(T);

    IQueryProvider IQueryable.Provider => this.provider;

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)this.provider.Execute(this.expression)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)this.provider.Execute(this.expression)).GetEnumerator();
    }

    public override string ToString()
    {
        return this.provider.GetQueryText(this.expression);
    }
}
