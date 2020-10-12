using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Sprache;

namespace CQuery
{
  public static class ParameterQuery
  {
    public static ParameterQueryBuilder<T> Build<T>() => new ParameterQueryBuilder<T>();

    public class ParameterQueryBuilder<T>
    {
      private readonly Dictionary<string, Func<T, string>> _getters = new Dictionary<string, Func<T, string>>();
      private Func<T, string> _freeGetter;
      private DynamicParameterHandler _dynamicGetter;

      private bool _caseSensitive = true;
      private bool _parameterCaseSensitive = true;
      private PhraseDelimiterMode _phraseDelimiterMode = PhraseDelimiterMode.Quotes;

      public delegate string DynamicParameterHandler(T obj, string parameter);

      public ParameterQueryBuilder<T> RegisterFreeText(Func<T, string> getter)
      {
        _freeGetter = getter;
        return this;
      }

      public ParameterQueryBuilder<T> RegisterParameter(string name, Func<T, string> getter)
      {
        _getters[name] = getter;
        return this;
      }

      public ParameterQueryBuilder<T> RegisterDynamicParameter(DynamicParameterHandler handler)
      {
        _dynamicGetter = handler;
        return this;
      }

      public ParameterQueryBuilder<T> IgnoreCase()
      {
        _caseSensitive = false;
        return this;
      }

      public ParameterQueryBuilder<T> IgnoreParameterCase()
      {
        _parameterCaseSensitive = false;
        return this;
      }

      public ParameterQueryBuilder<T> QuoteDelimiters()
      {
        _phraseDelimiterMode = PhraseDelimiterMode.Quotes;
        return this;
      }

      public ParameterQueryBuilder<T> ParenthesesDelimiters()
      {
        _phraseDelimiterMode = PhraseDelimiterMode.Parentheses;
        return this;
      }

      [SuppressMessage("ReSharper", "InconsistentNaming")]
      [SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
      public CompiledParameterQuery<T> Compile(string query)
      {
        var inputObjExpr = Expression.Parameter(typeof(T), "obj");

        var getters = _parameterCaseSensitive
            ? _getters
            : _getters.ToDictionary(x => x.Key.ToLower(), x => x.Value);

        Expression CreateExpression(string parameter, string phrase)
        {
          Expression phraseExpr;

          if (parameter == null)
            phraseExpr = Expression.Call(Expression.Constant(_freeGetter.Target), _freeGetter.Method, inputObjExpr);
          else if (getters.TryGetValue(_parameterCaseSensitive ? parameter : parameter.ToLower(), out var getter))
            phraseExpr = Expression.Call(Expression.Constant(getter.Target), getter.Method, inputObjExpr);
          else if (_dynamicGetter == null)
            throw new ParserException($"parameter '{parameter}' is not registered");
          else
            phraseExpr = Expression.Call(Expression.Constant(_dynamicGetter.Target), _dynamicGetter.Method, inputObjExpr, Expression.Constant(parameter));

          var opts = RegexOptions.Compiled;
          if (!_caseSensitive)
            opts |= RegexOptions.IgnoreCase;

          var regex = new Regex(@$"\b{Regex.Escape(phrase)}\b", opts);
          return Expression.Call(Expression.Constant(regex), "IsMatch", null, phraseExpr);
        }

        Parser<string> Identifier = Parse.Identifier(Parse.Letter, Parse.LetterOrDigit.Or(Parse.Char('_'))).Text();
        Parser<string> Phrase = Helpers.CreatePhraseParser(_phraseDelimiterMode);

        var ParsedPhrase =
            from identifier in Identifier.Then(x => Parse.Char(':').Return(x)).Optional()
            from phrase in Phrase
            select CreateExpression(identifier.GetOrDefault(), phrase);

        var result = LogicParserBuilder.BuildLogicParser(ParsedPhrase).TryParse(query);

        if (result.WasSuccessful) {
          var lambda = Expression.Lambda<Func<T, bool>>(result.Value, inputObjExpr);
          return new CompiledParameterQuery<T>(lambda.Compile());
        }
        else {
          var msg = result.Message + ", expectations: " + string.Join(", ", result.Expectations);
          throw new ParserException(msg);
        }
      }
    }
  }

  public class CompiledParameterQuery<T>
  {
    private readonly Func<T, bool> _lambda;

    public CompiledParameterQuery(Func<T, bool> lambda)
    {
      _lambda = lambda;
    }

    public bool IsMatch(T obj)
    {
      return _lambda(obj);
    }
  }
}