using System.Collections.Generic;
using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    /// <summary>
    /// Unit tests for CSS variable extraction and replacement in <see cref="BaseHtmlProvider"/>.
    /// </summary>
    public class BaseHtmlProviderTest
    {
        private readonly BaseHtmlProvider _provider;

        public BaseHtmlProviderTest()
        {
            _provider = new BaseHtmlProvider();
        }

        #region ExtractRootCssVariables Tests

        [Fact]
        public void ExtractRootCssVariables_BasicVariable_ExtractsCorrectly()
        {
            var html = @"
:root {
    --spacing: 20px;
}
";
            var result = _provider.ExtractRootCssVariables(html);

            Assert.Single(result);
            Assert.Equal("20px", result["spacing"]);
        }

        [Fact]
        public void ExtractRootCssVariables_MultipleVariables_ExtractsAll()
        {
            var html = @"
:root {
    --spacing: 20px;
    --color: #fff;
    --font-size: 14px;
}
";
            var result = _provider.ExtractRootCssVariables(html);

            Assert.Equal(3, result.Count);
            Assert.Equal("20px", result["spacing"]);
            Assert.Equal("#fff", result["color"]);
            Assert.Equal("14px", result["font-size"]);
        }

        [Fact]
        public void ExtractRootCssVariables_FontFamilyWithCommas_ExtractsCorrectly()
        {
            var html = @"
:root {
    --default-font: 'Segoe UI', Tahoma, Geneva, sans-serif;
}
";
            var result = _provider.ExtractRootCssVariables(html);

            Assert.Single(result);
            Assert.Equal("'Segoe UI', Tahoma, Geneva, sans-serif", result["default-font"]);
        }

        [Fact]
        public void ExtractRootCssVariables_MultipleRootBlocks_ExtractsFromAll()
        {
            var html = @"
:root {
    --first: 10px;
}
body { color: red; }
:root {
    --second: 20px;
}
";
            var result = _provider.ExtractRootCssVariables(html);

            Assert.Equal(2, result.Count);
            Assert.Equal("10px", result["first"]);
            Assert.Equal("20px", result["second"]);
        }

        [Fact]
        public void ExtractRootCssVariables_DuplicateVariable_LastDefinitionWins()
        {
            var html = @"
:root {
    --spacing: 10px;
    --spacing: 20px;
}
";
            var result = _provider.ExtractRootCssVariables(html);

            Assert.Single(result);
            Assert.Equal("20px", result["spacing"]);
        }

        [Fact]
        public void ExtractRootCssVariables_NoRootBlock_ReturnsEmpty()
        {
            var html = @"
body {
    --not-root: 10px;
}
";
            var result = _provider.ExtractRootCssVariables(html);

            Assert.Empty(result);
        }

        [Fact]
        public void ExtractRootCssVariables_SingleLineBlockComment_IgnoresCommentedVariable()
        {
            var html = @"
:root {
    --spacing: 20px;
    /* --commented: 30px; */
}
";
            var result = _provider.ExtractRootCssVariables(html);

            Assert.Single(result);
            Assert.Equal("20px", result["spacing"]);
            Assert.False(result.ContainsKey("commented"));
        }

        [Fact]
        public void ExtractRootCssVariables_MultiLineBlockComment_IgnoresCommentedVariables()
        {
            var html = @"
:root {
    --before: 10px;
    /*
    --commented1: 20px;
    --commented2: 30px;
    */
    --after: 40px;
}
";
            var result = _provider.ExtractRootCssVariables(html);

            Assert.Equal(2, result.Count);
            Assert.Equal("10px", result["before"]);
            Assert.Equal("40px", result["after"]);
            Assert.False(result.ContainsKey("commented1"));
            Assert.False(result.ContainsKey("commented2"));
        }

        [Fact]
        public void ExtractRootCssVariables_VariableWithInlineComment_ExtractsValue()
        {
            var html = @"
:root {
    --spacing: 20px; /* inline comment */
}
";
            var result = _provider.ExtractRootCssVariables(html);

            Assert.Single(result);
            Assert.Equal("20px", result["spacing"]);
        }

        [Fact]
        public void ExtractRootCssVariables_MultipleBlockComments_IgnoresAll()
        {
            var html = @"
:root {
    /* first comment */
    --real1: 10px;
    /* --fake: 20px; */
    --real2: 30px;
    /* another comment */
}
";
            var result = _provider.ExtractRootCssVariables(html);

            Assert.Equal(2, result.Count);
            Assert.Equal("10px", result["real1"]);
            Assert.Equal("30px", result["real2"]);
            Assert.False(result.ContainsKey("fake"));
        }

        #endregion

        #region ReplaceCssVarUsages Tests

        [Fact]
        public void ReplaceCssVarUsages_BasicReplacement_ReplacesCorrectly()
        {
            var html = "margin: var(--spacing);";
            var varMap = new Dictionary<string, string> { { "spacing", "20px" } };

            var result = _provider.ReplaceCssVarUsages(html, varMap, 10);

            Assert.Equal("margin: 20px;", result);
        }

        [Fact]
        public void ReplaceCssVarUsages_MultipleVariables_ReplacesAll()
        {
            var html = "margin: var(--spacing); color: var(--color);";
            var varMap = new Dictionary<string, string>
            {
                { "spacing", "20px" },
                { "color", "#fff" }
            };

            var result = _provider.ReplaceCssVarUsages(html, varMap, 10);

            Assert.Equal("margin: 20px; color: #fff;", result);
        }

        [Fact]
        public void ReplaceCssVarUsages_WithFallback_UsesFallbackWhenNotInMap()
        {
            var html = "margin: var(--unknown, 15px);";
            var varMap = new Dictionary<string, string>();

            var result = _provider.ReplaceCssVarUsages(html, varMap, 10);

            Assert.Equal("margin: 15px;", result);
        }

        [Fact]
        public void ReplaceCssVarUsages_WithFallback_PrefersMapValueOverFallback()
        {
            var html = "margin: var(--spacing, 15px);";
            var varMap = new Dictionary<string, string> { { "spacing", "20px" } };

            var result = _provider.ReplaceCssVarUsages(html, varMap, 10);

            Assert.Equal("margin: 20px;", result);
        }

        [Fact]
        public void ReplaceCssVarUsages_NoFallbackNotInMap_LeavesUnchanged()
        {
            var html = "margin: var(--unknown);";
            var varMap = new Dictionary<string, string>();

            var result = _provider.ReplaceCssVarUsages(html, varMap, 10);

            Assert.Equal("margin: var(--unknown);", result);
        }

        [Fact]
        public void ReplaceCssVarUsages_NestedVariables_ResolvesInMultiplePasses()
        {
            var html = "margin: var(--outer);";
            var varMap = new Dictionary<string, string>
            {
                { "outer", "var(--inner)" },
                { "inner", "20px" }
            };

            var result = _provider.ReplaceCssVarUsages(html, varMap, 10);

            Assert.Equal("margin: 20px;", result);
        }

        [Fact]
        public void ReplaceCssVarUsages_DeeplyNestedVariables_ResolvesWithinLimit()
        {
            var html = "margin: var(--level1);";
            var varMap = new Dictionary<string, string>
            {
                { "level1", "var(--level2)" },
                { "level2", "var(--level3)" },
                { "level3", "var(--level4)" },
                { "level4", "20px" }
            };

            var result = _provider.ReplaceCssVarUsages(html, varMap, 10);

            Assert.Equal("margin: 20px;", result);
        }

        [Fact]
        public void ReplaceCssVarUsages_MaxIterationsReached_StopsEarly()
        {
            var html = "margin: var(--level1);";
            var varMap = new Dictionary<string, string>
            {
                { "level1", "var(--level2)" },
                { "level2", "var(--level3)" },
                { "level3", "var(--level4)" },
                { "level4", "20px" }
            };

            // Only allow 2 iterations - won't fully resolve
            var result = _provider.ReplaceCssVarUsages(html, varMap, 2);

            Assert.Equal("margin: var(--level3);", result);
        }

        [Fact]
        public void ReplaceCssVarUsages_NoVariables_ReturnsUnchanged()
        {
            var html = "margin: 20px;";
            var varMap = new Dictionary<string, string> { { "spacing", "10px" } };

            var result = _provider.ReplaceCssVarUsages(html, varMap, 10);

            Assert.Equal("margin: 20px;", result);
        }

        [Fact]
        public void ReplaceCssVarUsages_FallbackWithSpaces_TrimsFallback()
        {
            var html = "margin: var(--unknown,   15px  );";
            var varMap = new Dictionary<string, string>();

            var result = _provider.ReplaceCssVarUsages(html, varMap, 10);

            Assert.Equal("margin: 15px;", result);
        }

        #endregion
    }
}
