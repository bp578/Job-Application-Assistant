using JobAssistant.Api.Ingestion;

namespace JobAssistant.Tests;

public class HtmlTextTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToPlainText_ReturnsNull_ForEmptyInput(string? html)
    {
        Assert.Null(HtmlText.ToPlainText(html));
    }

    [Fact]
    public void ToPlainText_DecodesEscapedHtml_AndStripsTags()
    {
        // Greenhouse's format: the HTML is itself HTML-escaped.
        const string escaped = "&lt;p&gt;Build &amp;amp; ship&lt;/p&gt;&lt;ul&gt;&lt;li&gt;C#&lt;/li&gt;&lt;/ul&gt;";

        Assert.Equal("Build & ship\nC#", HtmlText.ToPlainText(escaped));
    }

    [Fact]
    public void ToPlainText_HandlesPlainHtml_AndCollapsesWhitespace()
    {
        const string html = "<h2>About   the role</h2>\n\n<p>Work  on <strong>APIs</strong>.</p><br/>";

        Assert.Equal("About the role\nWork on APIs.", HtmlText.ToPlainText(html));
    }
}
