using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Sprache;

namespace CQuery
{
  public static class SimpleQuery
  {
    public class Options
    {
      public bool CaseSensitive { get; set; } = true;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static Func<string, bool> Compile(string query, Options options = null)
    {
      options ??= new Options();

      var inputParam = Expression.Parameter(typeof(string), "text");

      Parser<Expression> ParsedPhrase =
          Helpers.CreatePhraseParser('"', '"')
                 .Select(x => {
                   var opts = RegexOptions.Compiled;
                   if (!options.CaseSensitive)
                     opts |= RegexOptions.IgnoreCase;

                   var regex = new Regex($@"\b{Regex.Escape(x)}\b", opts);
                   return Expression.Call(Expression.Constant(regex), "IsMatch", null, inputParam);
                 });

      var result = LogicParserBuilder.BuildLogicParser(ParsedPhrase).TryParse(query);

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