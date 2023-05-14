using System.Linq.Expressions;
using System.Text;
// You can define other methods, fields, classes and namespaces here

internal class QueryTranslator : ExpressionVisitor
{
    private StringBuilder sb;
    private int? top = null;

    internal QueryTranslator() { }

    internal string Translate(Expression expression)
    {
        this.sb = new StringBuilder();
        this.Visit(expression);
        return this.sb.ToString();
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
                sb.Append("SELECT * FROM (");
                this.Visit(m.Arguments[0]);
                sb.Append(") AS T WHERE ");
                var lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                this.Visit(lambda.Body);
                return m;
            case "Take":
                sb.Append("SELECT TOP ");
                var value = (ConstantExpression)(m.Arguments[1]);
                this.VisitTopConstant(value);
                sb.Append(" * FROM (");
                this.Visit(m.Arguments[0]);
                sb.Append(") AS U");
                return m;
        }

        throw new NotSupportedException($"The method '{m.Method.Name}' is not supported");
    }

    protected override Expression VisitUnary(UnaryExpression u)
    {
        switch (u.NodeType)
        {
            case ExpressionType.Not:
                sb.Append(" NOT ");
                this.Visit(u.Operand);
                break;
            default:
                throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
        }

        return u;
    }

    protected override Expression VisitBinary(BinaryExpression b)
    {
        sb.Append("(");
        this.Visit(b.Left);
        switch (b.NodeType)
        {
            case ExpressionType.And:
                sb.Append(" AND ");
                break;
            case ExpressionType.Or:
                sb.Append(" OR");
                break;
            case ExpressionType.Equal:
                sb.Append(" = ");
                break;
            case ExpressionType.NotEqual:
                sb.Append(" <> ");
                break;
            case ExpressionType.LessThan:
                sb.Append(" < ");
                break;
            case ExpressionType.LessThanOrEqual:
                sb.Append(" <= ");
                break;
            case ExpressionType.GreaterThan:
                sb.Append(" > ");
                break;
            case ExpressionType.GreaterThanOrEqual:
                sb.Append(" >= ");
                break;
            default:
                throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));
        }

        this.Visit(b.Right);
        sb.Append(")");
        return b;
    }

    protected Expression VisitTopConstant(ConstantExpression c)
    {
        switch (Type.GetTypeCode(c.Value.GetType()))
        {
            case TypeCode.Int32:
                sb.Append((int)c.Value);
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
            sb.Append("NULL");
        }
        else if (c.Value is IQueryable q)
        {
            // assume constant nodes w/ IQueryables are table references
            sb.Append("SELECT * FROM ");
            sb.Append(q.ElementType.Name);
        }
        else
        {
            switch (Type.GetTypeCode(c.Value.GetType()))
            {
                case TypeCode.Boolean:
                    sb.Append(((bool)c.Value) ? 1 : 0);
                    break;
                case TypeCode.String:
                    sb.Append("'");
                    sb.Append(c.Value);
                    sb.Append("'");
                    break;
                case TypeCode.Object:
                    throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", c.Value));
                default:
                    sb.Append(c.Value);
                    break;
            }
        }
        return c;
    }

    protected override Expression VisitMember(MemberExpression m)
    {
        if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter)
        {
            sb.Append(m.Member.Name);
            return m;
        }

        throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
    }
}
