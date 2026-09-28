using CdaCrImg.Serialization;

namespace CdaCrImg.Tests;

public class Hl7FormatTests
{
    [Theory]
    [InlineData(1, 0, "20210108111700+0100")]
    [InlineData(0, 0, "20210108111700+0000")]
    [InlineData(-5, -30, "20210108111700-0530")]
    [InlineData(5, 45, "20210108111700+0545")]
    public void Timestamp_FormatsOffset(int hours, int minutes, string expected)
    {
        var value = new DateTimeOffset(2021, 1, 8, 11, 17, 0, new TimeSpan(hours, minutes, 0));

        Assert.Equal(expected, Hl7Format.Timestamp(value));
    }

    [Fact]
    public void Date_FormatsDayOnly()
    {
        Assert.Equal("19790328", Hl7Format.Date(new DateTime(1979, 3, 28, 23, 59, 0)));
    }
}
