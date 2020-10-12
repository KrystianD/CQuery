using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Sprache;

namespace CQuery
{
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal static class LogicParserBuilder
  {
    private static readonly Parser<ExpressionType> LogicBinary = Parse.Or(
        Parse.String("AND").Token().Return(ExpressionType.And),
        Parse.String("OR").Token().Return(ExpressionType.Or));

    private static readonly Parser<ExpressionType> LogicNot =
        Parse.String("NOT").Token().Return(ExpressionType.Not);

    public static Parser<Expression> BuildLogicParser(Parser<Expression> phrase)
    {
      Parser<Expression> Expr = null;
      Parser<Expression> Factor = null;

      Factor =
          Parse.Ref(() => Expr).Contained(Parse.Char('('), Parse.Char(')'))
               .XOr(from _ in LogicNot
                    from expr in Factor
                    select Expression.Not(expr))
               .XOr(phrase.Named("<phrase>"));

      Expr = Parse.XChainOperator(LogicBinary, Factor, Expression.MakeBinary);

      return Expr.End();
    }
  }
}