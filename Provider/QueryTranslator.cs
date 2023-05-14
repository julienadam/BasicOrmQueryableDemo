using System.Linq.Expressions;
using System.Text;
// You can define other methods, fields, classes and namespaces here

internal class QueryState
{
    public string From { get; set; } = "";
    public int? Top { get; set; } = null;
    public StringBuilder Where { get; } = new StringBuilder();

    public string BuildQuery()
    {
        var topClause = Top == null ? string.Empty : $"TOP {Top}";
        return $"SELECT {topClause} *  FROM {From} WHERE {Where}";
    }
}

internal class QueryTranslator : ExpressionVisitor
{
    private readonly QueryState state = new QueryState();

    internal QueryTranslator() { }

    internal string Translate(Expression expression)
    {
        this.Visit(expression);
        return state.BuildQuery();
    }

    private static Expression StripQuotes(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
        {
            e = ((UnaryExpression)e).Operand;
        }

        return e;
    }

    protected override Expression VisitMethodCall(MethodCallExpression m)
    {
        if (m.Method.DeclaringType != typeof(Queryable))
            throw new NotSupportedException($"The method '{m.Method.Name}' is not supported");

        switch (m.Method.Name)
        {
            case "Where":
                this.Visit(m.Arguments[0]);
                var lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                this.Visit(lambda.Body);
                return m;
            case "Take":
                var value = (ConstantExpression)(m.Arguments[1]);
                this.VisitTopConstant(value);
                this.Visit(m.Arguments[0]);
                return m;
        }

        throw new NotSupportedException($"The method '{m.Method.Name}' is not supported");
    }

    protected override Expression VisitUnary(UnaryExpression u)
    {
        switch (u.NodeType)
        {
            case ExpressionType.Not:
                state.Where.Append(" NOT ");
                this.Visit(u.Operand);
                break;
            default:
                throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
        }

        return u;
    }

    protected override Expression VisitBinary(BinaryExpression b)
    {
        state.Where.Append("(");
        this.Visit(b.Left);
        switch (b.NodeType)
        {
            case ExpressionType.And:
                state.Where.Append(" AND ");
                break;
            case ExpressionType.AndAlso:
                state.Where.Append(" AND ");
                break;
            case ExpressionType.Or:
                state.Where.Append(" OR");
                break;
            case ExpressionType.Equal:
                state.Where.Append(" = ");
                break;
            case ExpressionType.NotEqual:
                state.Where.Append(" <> ");
                break;
            case ExpressionType.LessThan:
                state.Where.Append(" < ");
                break;
            case ExpressionType.LessThanOrEqual:
                state.Where.Append(" <= ");
                break;
            case ExpressionType.GreaterThan:
                state.Where.Append(" > ");
                break;
            case ExpressionType.GreaterThanOrEqual:
                state.Where.Append(" >= ");
                break;
            default:
                throw new NotSupportedException($"The binary operator '{b.NodeType}' is not supported");
        }

        this.Visit(b.Right);
        state.Where.Append(")");
        return b;
    }

    protected Expression VisitTopConstant(ConstantExpression c)
    {
        switch (Type.GetTypeCode(c.Value.GetType()))
        {
            case TypeCode.Int32:
                state.Top = (int)c.Value;
                break;
            default:
                throw new InvalidCastException("Not a valid integer");
        }

        return c;
    }

    protected override Expression VisitConstant(ConstantExpression c)
    {
        if (c.Value == null)
        {
            state.Where.Append("NULL");
        }
        else if (c.Value is IQueryable q)
        {
            // assume constant nodes w/ IQueryables are table references
            state.From = q.ElementType.Name;
        }
        else
        {
            switch (Type.GetTypeCode(c.Value.GetType()))
            {
                case TypeCode.Boolean:
                    state.Where.Append(((bool)c.Value) ? 1 : 0);
                    break;
                case TypeCode.String:
                    state.Where.Append("'");
                    state.Where.Append(c.Value);
                    state.Where.Append("'");
                    break;
                case TypeCode.Object:
                    throw new NotSupportedException($"The constant for '{c.Value}' is not supported");
                default:
                    state.Where.Append(c.Value);
                    break;
            }
        }
        return c;
    }

    protected override Expression VisitMember(MemberExpression m)
    {
        if (m.Expression is not { NodeType: ExpressionType.Parameter })
            throw new NotSupportedException($"The member '{m.Member.Name}' is not supported");

        state.Where.Append(m.Member.Name);
        return m;
    }
}
