using Xunit;

namespace CQuery.Test
{
  public class SimpleQueryTest
  {
    [Fact]
    public void TestSimple()
    {
      var matcher = SimpleQuery.Compile(@"""word1""");

      Assert.True(matcher("word1"));
      Assert.False(matcher("word2"));
    }

    [Fact]
    public void TestSimplePhrase()
    {
      var matcher = SimpleQuery.Compile(@"""multi word""");

      Assert.True(matcher("multi word"));
      Assert.True(matcher("inner multi word inner"));
      Assert.False(matcher("inner multi test word inner"));
    }

    [Fact]
    public void TestSingleCondition()
    {
      var matcher = SimpleQuery.Compile(@"""word1"" OR ""word2""");

      Assert.True(matcher("word1"));
      Assert.True(matcher("word2"));
      Assert.False(matcher("word3"));
    }

    [Fact]
    public void TestMultipleConditions()
    {
      var matcher = SimpleQuery.Compile(@"(""word1"" OR ""word2"") AND ""word3""");

      Assert.True(matcher("word1 word3"));
      Assert.True(matcher("word2 word3"));
      Assert.False(matcher("word3"));
    }

    [Fact]
    public void TestNotCondition()
    {
      var matcher = SimpleQuery.Compile(@"NOT ""word3""");

      Assert.True(matcher("word1"));
      Assert.False(matcher("word3"));
    }

    [Fact]
    public void TestCaseSensitive()
    {
      var matcher = SimpleQuery.Compile(@"""word1""");

      Assert.True(matcher("word1"));
      Assert.False(matcher("Word1"));
    }

    [Fact]
    public void TestCaseInsensitive()
    {
      var matcher = SimpleQuery.Compile(@"""word1"" OR ""Word2""", new SimpleQuery.Options() {
          CaseSensitive = false,
      });

      Assert.True(matcher("word1"));
      Assert.True(matcher("Word1"));
      Assert.True(matcher("word2"));
      Assert.True(matcher("Word2"));
      Assert.False(matcher("Word3"));
    }
  }
}