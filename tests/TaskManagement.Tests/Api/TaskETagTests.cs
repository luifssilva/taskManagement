using TaskManagement.Api.Http;

namespace TaskManagement.Tests.Api;

public class TaskETagTests
{
    [Fact]
    public void From_QuotesTheVersion()
    {
        Assert.Equal("\"3\"", TaskETag.From(3));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("*", null)]
    [InlineData("\"3\"", 3)]
    [InlineData("W/\"3\"", 3)]
    [InlineData(" \"12\" ", 12)]
    [InlineData("\"abc\"", 0)]
    [InlineData("\"-1\"", 0)]
    public void ExpectedVersion_ParsesIfMatch(string? ifMatch, int? expected)
    {
        Assert.Equal(expected, TaskETag.ExpectedVersion(ifMatch));
    }
}
