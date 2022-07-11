using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        private readonly StringBuilder _resultStringBuilder;

        public ExpressionToFtsRequestTranslator()
        {
            _resultStringBuilder = new StringBuilder();
        }

        public string Translate(Expression exp)
        {
            Visit(exp);

            return _resultStringBuilder.ToString();
        }

        #region protected methods

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable)
                && node.Method.Name == "Where")
            {
                var predicate = node.Arguments[1];
                Visit(predicate);

                return node;
            }

            if (node.Method.DeclaringType == typeof(string))
            {
                switch (node.Method.Name)
                {
                    case "Equals":
                        return StringOperationsResultBuilder(node, "(", ")");
                    case "StartsWith":
                        return StringOperationsResultBuilder(node, "(", "*)");
                    case "EndsWith":
                        return StringOperationsResultBuilder(node, "(*", ")");
                    case "Contains":
                        return StringOperationsResultBuilder(node, "(*", "*)");
                    default:
                        throw new NotSupportedException($"Method {node.Method.Name} is not supported");
                }
            }

            return base.VisitMethodCall(node);
        }

        private Expression StringOperationsResultBuilder(MethodCallExpression node, string openingSymbols, string closingSymbols)
        {
            Visit(node.Object);
            _resultStringBuilder.Append(openingSymbols);
            Visit(node.Arguments[0]);
            _resultStringBuilder.Append(closingSymbols);
            return node;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    VisitBinaryEqual(node);
                    break;
                case ExpressionType.AndAlso:
                    VisitBinaryAnd(node);
                    break;
                default:
                    throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            };

            return node;
        }

        private void VisitBinaryEqual(BinaryExpression node)
        {
            var memberAccess = GetMemberAccess(node);
            var constant = GetConstant(node);
            if (memberAccess is null || constant is null)
            {
                throw new NotSupportedException(
                    $"Expression should consist of constant and property or field: {node.NodeType}");
            }

            Visit(memberAccess);
            _resultStringBuilder.Append("(");
            Visit(constant);
            _resultStringBuilder.Append(")");
        }

        private void VisitBinaryAnd(BinaryExpression node)
        {
            Visit(node.Left);
            _resultStringBuilder.Append(" And ");
            Visit(node.Right);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name).Append(":");

            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _resultStringBuilder.Append(node.Value);

            return node;
        }

        private Expression GetMemberAccess(BinaryExpression node)
        {
            if (node.Left.NodeType == ExpressionType.MemberAccess)
            {
                return node.Left;
            }

            if (node.Right.NodeType == ExpressionType.MemberAccess)
            {
                return node.Right;
            }

            return null;
        }

        private Expression GetConstant(BinaryExpression node)
        {
            if (node.Left.NodeType == ExpressionType.Constant)
            {
                return node.Left;
            }

            if (node.Right.NodeType == ExpressionType.Constant)
            {
                return node.Right;
            }

            return null;
        }

        #endregion
    }
}
