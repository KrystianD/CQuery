CQuery
======

Query language for C#

**Examples**

```c#
var matcher = SimpleQuery.Compile(@"(""word1"" OR ""word2"") AND ""word3""");

matcher("word1 word3"); // -> true
matcher("word2 word3"); // -> true
matcher("word3"); // -> false
matcher("word4"); // -> false
```