using Anhei4Map.Core.Services;

namespace Anhei4Map.Tests;

public class DomainPolicyTests
{
    [Fact]
    public void Allows_HttpsRootDomain()
    {
        Assert.True(DomainPolicy.IsAllowed("https://helltides.com/"));
    }

    [Fact]
    public void Allows_HttpsWww()
    {
        Assert.True(DomainPolicy.IsAllowed("https://www.helltides.com/"));
    }

    [Fact]
    public void Allows_HostCaseInsensitive()
    {
        Assert.True(DomainPolicy.IsAllowed("https://HELLTIDES.COM/map"));
    }

    [Fact]
    public void Allows_LegitPath()
    {
        Assert.True(DomainPolicy.IsAllowed("https://helltides.com/map/live"));
    }

    [Fact]
    public void Allows_LegitQuery()
    {
        Assert.True(DomainPolicy.IsAllowed("https://helltides.com/?region=us"));
    }

    [Fact]
    public void Allows_LegitFragment()
    {
        Assert.True(DomainPolicy.IsAllowed("https://helltides.com/#events"));
    }

    [Fact]
    public void Blocks_Http()
    {
        Assert.False(DomainPolicy.IsAllowed("http://helltides.com/"));
    }

    [Fact]
    public void Blocks_EvilSubdomain()
    {
        Assert.False(DomainPolicy.IsAllowed("https://evil.helltides.com/"));
    }

    [Fact]
    public void Blocks_LookalikeSuffix()
    {
        Assert.False(DomainPolicy.IsAllowed("https://helltides.com.example.com/"));
    }

    [Fact]
    public void Blocks_UserinfoSpoof()
    {
        Assert.False(DomainPolicy.IsAllowed("https://helltides.com@evil.example/"));
    }

    [Fact]
    public void Blocks_FileScheme()
    {
        Assert.False(DomainPolicy.IsAllowed("file:///C:/Windows/System32/drivers/etc/hosts"));
    }

    [Fact]
    public void Blocks_JavascriptScheme()
    {
        Assert.False(DomainPolicy.IsAllowed("javascript:alert(1)"));
    }

    [Fact]
    public void Blocks_DataScheme()
    {
        Assert.False(DomainPolicy.IsAllowed("data:text/html,<script>alert(1)</script>"));
    }

    [Fact]
    public void Blocks_Localhost()
    {
        Assert.False(DomainPolicy.IsAllowed("https://localhost/"));
    }

    [Fact]
    public void Blocks_IPv4()
    {
        Assert.False(DomainPolicy.IsAllowed("https://93.184.216.34/"));
    }

    [Fact]
    public void Blocks_Non443Port()
    {
        Assert.False(DomainPolicy.IsAllowed("https://helltides.com:8443/"));
    }

    [Fact]
    public void Blocks_InvalidUri()
    {
        Assert.False(DomainPolicy.IsAllowed("not a valid uri"));
    }

    [Fact]
    public void Blocks_Null()
    {
        Assert.False(DomainPolicy.IsAllowed(null!));
    }

    [Fact]
    public void Blocks_EmptyString()
    {
        Assert.False(DomainPolicy.IsAllowed(""));
    }

    [Fact]
    public void Allows_Default443Port()
    {
        Assert.True(DomainPolicy.IsAllowed("https://helltides.com:443/map"));
    }

    [Fact]
    public void Blocks_UserinfoOnRootDomain()
    {
        Assert.False(DomainPolicy.IsAllowed("https://user:pass@helltides.com/"));
    }

    [Fact]
    public void Blocks_UserinfoOnWwwDomain()
    {
        Assert.False(DomainPolicy.IsAllowed("https://user:pass@www.helltides.com/"));
    }

    [Fact]
    public void Blocks_UserinfoOnLegitPath()
    {
        Assert.False(DomainPolicy.IsAllowed("https://x@helltides.com/map"));
    }
}
