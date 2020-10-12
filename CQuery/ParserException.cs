using System;

namespace CQuery
{
  public class ParserException : Exception
  {
    public ParserException(string message) : base(message) { }
  }
}