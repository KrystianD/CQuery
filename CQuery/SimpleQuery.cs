using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Sprache;

namespace CQuery
{
  public class SimpleQueryOptions
  {
    public bool CaseInsensitive { get; set; } = false;
  }

  public static class SimpleQuery
  {
    private static Parser<ExpressionType> LogicBinary = Parse.Or(
        Parse.String("AND").Token().Return(ExpressionType.And),
        Parse.String("OR").Token().Return(ExpressionType.Or));

    private static Parser<ExpressionType> LogicNot =
        Parse.String("NOT").Token().Return(ExpressionType.Not);

    private static readonly Parser<string> Phrase = Parse.CharExcept('"').AtLeastOnce().Contained(Parse.Char('"'), Parse.Char('"')).Text();

    public static Func<string, bool> Compile(string query, SimpleQueryOptions options = null)
    {
      if (options == null)
        options = new SimpleQueryOptions();

      var inputParam = Expression.Parameter(typeof(string), "text");

      Parser<Expression> ParsedPhrase = Phrase
          .Select(x => {
            var opts = RegexOptions.Compiled;
            if (options.CaseInsensitive)
              opts |= RegexOptions.IgnoreCase;

            var regex = new Regex($@"\b{Regex.Escape(x)}\b", opts);
            return Expression.Call(Expression.Constant(regex), "IsMatch", null, inputParam);
          });

      Parser<Expression> Expr = null;

      Parser<Expression> Factor =
          Parse.Ref(() => Expr).Contained(Parse.Char('('), Parse.Char(')'))
               .XOr(from _ in LogicNot
                    from expr in Expr
                    select Expression.Not(expr))
               .XOr(ParsedPhrase);

      Expr = Parse.ChainOperator(LogicBinary, Factor, Expression.MakeBinary);

      var result = Expr.End().TryParse(query);

      if (result.WasSuccessful) {
        var lambda = Expression.Lambda<Func<string, bool>>(result.Value, inputParam);
        return lambda.Compile();
      }
      else {
        var msg = result.Message + ", expectations: " + string.Join(", ", result.Expectations);
        throw new ParserException(msg);
      }
    }
  }
}