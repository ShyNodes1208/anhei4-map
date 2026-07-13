using Anhei4Map.Core.Services;

namespace Anhei4Map.Tests;

public class RetryPolicyTests
{
    [Fact]
    public void NextDelay_Attempt0_Returns0ms()
    {
        Assert.Equal(0, RetryPolicy.NextDelay(0));
    }

    [Fact]
    public void NextDelay_Attempt1_Returns1000ms()
    {
        Assert.Equal(1000, RetryPolicy.NextDelay(1));
    }

    [Fact]
    public void NextDelay_Attempt2_Returns2000ms()
    {
        Assert.Equal(2000, RetryPolicy.NextDelay(2));
    }

    [Fact]
    public void NextDelay_Attempt3_Returns4000ms()
    {
        Assert.Equal(4000, RetryPolicy.NextDelay(3));
    }

    [Fact]
    public void NextDelay_Attempt4_Returns8000ms()
    {
        Assert.Equal(8000, RetryPolicy.NextDelay(4));
    }

    [Fact]
    public void NextDelay_Attempt5_Returns30000ms()
    {
        Assert.Equal(30000, RetryPolicy.NextDelay(5));
    }

    [Fact]
    public void NextDelay_Attempt6Through9_Returns30000ms()
    {
        foreach (var attempt in new[] { 6, 7, 8, 9 })
        {
            Assert.Equal(30000, RetryPolicy.NextDelay(attempt));
        }
    }

    [Fact]
    public void NextDelay_Attempt10_ReturnsNull()
    {
        Assert.Null(RetryPolicy.NextDelay(10));
    }

    [Fact]
    public void NextDelay_AttemptNegative1_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicy.NextDelay(-1));
    }

    [Fact]
    public void NextDelay_Attempt11_ReturnsNull()
    {
        Assert.Null(RetryPolicy.NextDelay(11));
    }
}
