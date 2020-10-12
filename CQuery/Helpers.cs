using System.Diagnostics.CodeAnalysis;
using Sprache;

namespace CQuery
{
  public enum PhraseDelimiterMode
  {
    Quotes,
    Parentheses,
  };

  internal static class Helpers
  {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
    public static Parser<string> CreatePhraseParser(char startPhraseDelimiter, char endPhraseDelimiter)
    {
      var StartPhraseDelimiter = Parse.Char(startPhraseDelimiter);
      var EndPhraseDelimiter = Parse.Char(endPhraseDelimiter);
      var PhraseDelimiters = StartPhraseDelimiter.Or(EndPhraseDelimiter);

      return Parse.XOr(
          Parse.AnyChar.Except(PhraseDelimiters).AtLeastOnce().Contained(StartPhraseDelimiter, EndPhraseDelimiter).Named("phrase"),
          Parse.AnyChar.Except(PhraseDelimiters).Except(Parse.WhiteSpace).Except(Parse.Chars("()")).AtLeastOnce().Named("word")
      ).Text();
    }

    public static Parser<string> CreatePhraseParser(PhraseDelimiterMode delimiterMode)
    {
      if (delimiterMode == PhraseDelimiterMode.Quotes)
        return CreatePhraseParser('"', '"');
      else if (delimiterMode == PhraseDelimiterMode.Parentheses)
        return CreatePhraseParser('(', ')');
      else
        throw new ParseException("invalid delimiter mode");
    }
  }
}