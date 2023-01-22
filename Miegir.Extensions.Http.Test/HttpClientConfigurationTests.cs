using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Extensions;
using Microsoft.Extensions.Http.Mocks;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http;

[TestClass]
public class HttpClientConfigurationTests
{
    [TestMethod]
    public void HttpClientPropertiesShouldBeConfigurable()
    {
        var provider = new MockConfigurationProvider();

        provider.Set("Http:BaseAddress", "https://www.domain.com/1/");

        var configuration = new ConfigurationBuilder().Add(provider).Build();

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddHttpClient()
            .AddHttpClientConfiguration()
            .BuildServiceProvider();

        serviceProvider.GetRequiredService<HttpClient>().BaseAddress
            .Should().Be("https://www.domain.com/1/");

        provider.Set("Http:BaseAddress", "https://www.domain.com/2/");

        serviceProvider.GetRequiredService<HttpClient>().BaseAddress
            .Should().Be("https://www.domain.com/1/");

        provider.Load();

        serviceProvider.GetRequiredService<HttpClient>().BaseAddress
            .Should().Be("https://www.domain.com/2/");

        provider.GetAccessCount("Http:BaseAddress").Should().Be(2);
    }

    [TestMethod]
    public void HttpClientPropertiesShouldBeConfigurableFromCustomConfiguration()
    {
        var provider = new MockConfigurationProvider();

        provider.Set("BaseAddress", "https://www.domain.com/");

        var configuration = new ConfigurationBuilder().Add(provider).Build();

        using var serviceProvider = new ServiceCollection()
            .AddHttpClient()
            .AddHttpClientConfiguration(options => options.Configuration = configuration)
            .BuildServiceProvider();

        serviceProvider.GetRequiredService<HttpClient>().BaseAddress
            .Should().Be("https://www.domain.com/");
    }

    [TestMethod]
    public void HttpClientPropertiesShouldBeConfigurableFromCustomSectionName()
    {
        var provider = new MockConfigurationProvider();

        provider.Set("Custom:BaseAddress", "https://www.domain.com/");

        var configuration = new ConfigurationBuilder().Add(provider).Build();

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddHttpClient()
            .AddHttpClientConfiguration(options => options.GlobalConfigurationSectionName = "Custom")
            .BuildServiceProvider();

        serviceProvider.GetRequiredService<HttpClient>().BaseAddress
            .Should().Be("https://www.domain.com/");
    }

    [TestMethod]
    public void HttpClientPropertiesShouldBeConfigurableFromEmptySectionName()
    {
        var provider = new MockConfigurationProvider();

        provider.Set("BaseAddress", "https://www.domain.com/");

        var configuration = new ConfigurationBuilder().Add(provider).Build();

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddHttpClient()
            .AddHttpClientConfiguration(options => options.GlobalConfigurationSectionName = null)
            .BuildServiceProvider();

        serviceProvider.GetRequiredService<HttpClient>().BaseAddress
            .Should().Be("https://www.domain.com/");
    }

    [TestMethod]
    public void HttpClientHeadersShouldBeConfigurable()
    {
        var provider = new MockConfigurationProvider();

        provider.Set("Http:BaseAddress", "https://www.domain.com/");
        provider.Set("Http:Headers:Header1", "Value1");
        provider.Set("Http:Headers:Header2", "Value2");
        provider.Set("Http:Named:Headers:Header1", "NamedValue1");
        provider.Set("Http:Named:Headers:Header3:0", "3.0");
        provider.Set("Http:Named:Headers:Header3:1", "3.1");
        provider.Set("Http:Named:Headers:Header3:2", "3.2");

        var configuration = new ConfigurationBuilder().Add(provider).Build();

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddHttpClient()
            .AddHttpClientConfiguration()
            .BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        factory.CreateClient("Named").DefaultRequestHeaders
            .Should().BeEquivalentTo(new Dictionary<string, string[]>()
            {
                ["Header1"] = new[] { "NamedValue1" },
                ["Header2"] = new[] { "Value2" },
                ["Header3"] = new[] { "3.0", "3.1", "3.2" },
            });
    }

    [TestMethod]
    public void NamedHttpClientShouldBeConfigurable()
    {
        var provider = new MockConfigurationProvider();

        provider.Set("Http:Client1:BaseAddress", "https://www.domain.com/1/");
        provider.Set("Http:Client2:BaseAddress", "https://www.domain.com/2/");

        var configuration = new ConfigurationBuilder().Add(provider).Build();

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddHttpClient()
            .AddHttpClientConfiguration()
            .BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        factory.CreateClient("Client1").BaseAddress.Should().Be("https://www.domain.com/1/");
        factory.CreateClient("Client2").BaseAddress.Should().Be("https://www.domain.com/2/");

        provider.Set("Http:Client1:BaseAddress", "https://www.domain.com/3/");
        provider.Load();

        factory.CreateClient("Client1").BaseAddress.Should().Be("https://www.domain.com/3/");
        factory.CreateClient("Client2").BaseAddress.Should().Be("https://www.domain.com/2/");
    }

    [TestMethod]
    public void NamedHttpClientShouldInheritConfiguration()
    {
        var provider = new MockConfigurationProvider();

        provider.Set("Http:Timeout", "0:42");
        provider.Set("Http:Parent:BaseAddress", "https://www.domain.com/1/");

        var configuration = new ConfigurationBuilder().Add(provider).Build();

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddHttpClient()
            .AddHttpClientConfiguration()
            .BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        factory.CreateClient("Parent:Child").BaseAddress.Should().Be("https://www.domain.com/1/");
        factory.CreateClient("Parent:Child").Timeout.TotalMinutes.Should().Be(42);

        provider.Set("Http:Parent:Child:BaseAddress", "https://www.domain.com/2/");
        provider.Load();

        factory.CreateClient("Parent:Child").BaseAddress.Should().Be("https://www.domain.com/2/");
        factory.CreateClient("Parent:Child").Timeout.TotalMinutes.Should().Be(42);
    }

    [TestMethod]
    public void HttpClientBaseAddressUsageShouldBeValidated()
    {
        var provider = new MockConfigurationProvider();

        provider.Set("Http:Client1:BaseAddress", "42");
        provider.Set("Http:Client2:BaseAddress", "https://www.domain.com/1");

        var configuration = new ConfigurationBuilder().Add(provider).Build();

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddHttpClient()
            .AddHttpClientConfiguration()
            .BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        factory.Invoking(f => f.CreateClient())
            .Should()
            .ThrowExactly<OptionsValidationException>()
            .WithMessage("HttpClient: BaseAddress should not be null.")
            .Where(x => x.OptionsName == "Http:BaseAddress" && x.OptionsType == typeof(Uri));

        factory.Invoking(f => f.CreateClient("Client1"))
            .Should()
            .ThrowExactly<OptionsValidationException>()
            .WithMessage("HttpClient[Client1]: BaseAddress should be absolute.")
            .Where(x => x.OptionsName == "Http:Client1:BaseAddress" && x.OptionsType == typeof(Uri));

        factory.Invoking(f => f.CreateClient("Client2"))
            .Should()
            .ThrowExactly<OptionsValidationException>()
            .WithMessage("HttpClient[Client2]: BaseAddress should end with '/'.")
            .Where(x => x.OptionsName == "Http:Client2:BaseAddress" && x.OptionsType == typeof(Uri));
    }

    [TestMethod]
    public void HttpClientBaseAddressFormatShouldBeValidated()
    {
        var provider = new MockConfigurationProvider();

        provider.Set("Http:BaseAddress", "a:b");

        var configuration = new ConfigurationBuilder().Add(provider).Build();

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddHttpClient()
            .AddHttpClientConfiguration()
            .BuildServiceProvider();

        serviceProvider.Invoking(p => p.GetRequiredService<HttpClient>())
            .Should()
            .ThrowExactly<OptionsValidationException>()
            .WithMessage("HttpClient: BaseAddress is invalid. *")
            .Where(x => x.OptionsName == "Http:BaseAddress" && x.OptionsType == typeof(Uri));
    }

    [TestMethod]
    public void HttpClientTimeoutFormatShouldBeValidated()
    {
        var provider = new MockConfigurationProvider();

        provider.Set("Http:Timeout", "invalid");

        var configuration = new ConfigurationBuilder().Add(provider).Build();

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddHttpClient()
            .AddHttpClientConfiguration()
            .BuildServiceProvider();

        serviceProvider.Invoking(p => p.GetRequiredService<HttpClient>())
            .Should()
            .ThrowExactly<OptionsValidationException>()
            .WithMessage("HttpClient: Timeout is invalid. *")
            .Where(x => x.OptionsName == "Http:Timeout" && x.OptionsType == typeof(TimeSpan));
    }

    [TestMethod]
    public void HttpClientHandlerAllowAutoRedirectFormatShouldBeValidated()
    {
        var provider = new MockConfigurationProvider();

        provider.Set("Http:AllowAutoRedirect", "invalid");

        var configuration = new ConfigurationBuilder().Add(provider).Build();

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddHttpClient()
            .AddHttpClientConfiguration()
            .BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IHttpMessageHandlerFactory>();

        factory.Invoking(f => f.CreateHandler())
            .Should()
            .ThrowExactly<OptionsValidationException>()
            .WithMessage("HttpClient: AllowAutoRedirect is invalid. *")
            .Where(x => x.OptionsName == "Http:AllowAutoRedirect" && x.OptionsType == typeof(bool));
    }

    [TestMethod]
    public void HttpClientHandlerAllowAutoRedirectShouldBeConfigured()
    {
        var provider = new MockConfigurationProvider();

        provider.Set("Http:F:AllowAutoRedirect", "false");
        provider.Set("Http:T:AllowAutoRedirect", "true");

        var configuration = new ConfigurationBuilder().Add(provider).Build();

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddHttpClient()
            .AddHttpClientConfiguration()
            .BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IHttpMessageHandlerFactory>();

        factory.CreateHandler("F").GetInnerHandlerOfType<HttpClientHandler>()
            .AllowAutoRedirect.Should().BeFalse();
        factory.CreateHandler("T").GetInnerHandlerOfType<HttpClientHandler>()
            .AllowAutoRedirect.Should().BeTrue();
    }

    [TestMethod]
    public void HttpClientHandlerProxyShouldBeConfigured()
    {
        var provider = new MockConfigurationProvider();

        provider.Set("Http:ProxyUrl", "proxyurl");
        provider.Set("Http:ProxyUserName", "proxyname");
        provider.Set("Http:ProxyPassword", "proxypass");
        provider.Set("Http:ProxyDomain", "proxydomain");

        var configuration = new ConfigurationBuilder().Add(provider).Build();

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddHttpClient()
            .AddHttpClientConfiguration()
            .BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IHttpMessageHandlerFactory>();
        var handler = factory.CreateHandler().GetInnerHandlerOfType<HttpClientHandler>();
        var proxy = handler.Proxy.Should().BeOfType<WebProxy>().Subject;
        var credential = proxy.Credentials.Should().BeOfType<NetworkCredential>().Subject;

        proxy.Address.Should().Be("http://proxyurl/");
        credential.UserName.Should().Be("proxyname");
        credential.Password.Should().Be("proxypass");
        credential.Domain.Should().Be("proxydomain");
    }

    [TestMethod]
    public void HttpClientHandlerProxyShouldInherit()
    {
        var provider = new MockConfigurationProvider();

        provider.Set("Http:ProxyUrl", "proxyurl");
        provider.Set("Http:ProxyUserName", "proxyname");
        provider.Set("Http:ProxyPassword", "proxypass");
        provider.Set("Http:ProxyDomain", "proxydomain");

        var configuration = new ConfigurationBuilder().Add(provider).Build();

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddHttpClient()
            .AddHttpClientConfiguration()
            .BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IHttpMessageHandlerFactory>();
        var handler = factory.CreateHandler("Named").GetInnerHandlerOfType<HttpClientHandler>();
        var proxy = handler.Proxy.Should().BeOfType<WebProxy>().Subject;
        var credential = proxy.Credentials.Should().BeOfType<NetworkCredential>().Subject;

        proxy.Address.Should().Be("http://proxyurl/");
        credential.UserName.Should().Be("proxyname");
        credential.Password.Should().Be("proxypass");
        credential.Domain.Should().Be("proxydomain");
    }

    [TestMethod]
    public void HttpClientHandlerProxyShouldOverride()
    {
        var provider = new MockConfigurationProvider();

        provider.Set("Http:ProxyUrl", "proxyurl");
        provider.Set("Http:ProxyUserName", "proxyname");
        provider.Set("Http:ProxyPassword", "proxypass");
        provider.Set("Http:ProxyDomain", "proxydomain");
        provider.Set("Http:Named:ProxyUrl", "namedproxyurl");
        provider.Set("Http:Named:ProxyUserName", "namedproxyname");

        var configuration = new ConfigurationBuilder().Add(provider).Build();

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddHttpClient()
            .AddHttpClientConfiguration()
            .BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IHttpMessageHandlerFactory>();
        var handler = factory.CreateHandler("Named:Child").GetInnerHandlerOfType<HttpClientHandler>();
        var proxy = handler.Proxy.Should().BeOfType<WebProxy>().Subject;
        var credential = proxy.Credentials.Should().BeOfType<NetworkCredential>().Subject;

        proxy.Address.Should().Be("http://namedproxyurl/");
        credential.UserName.Should().Be("namedproxyname");
        credential.Password.Should().BeEmpty();
        credential.Domain.Should().BeEmpty();
    }

    [TestMethod]
    public void HttpClientHandlerProxyShouldNotInheritWhenBaseAddressIsOverridden()
    {
        var provider = new MockConfigurationProvider();

        provider.Set("Http:ProxyUrl", "proxyurl");
        provider.Set("Http:ProxyUserName", "proxyname");
        provider.Set("Http:ProxyPassword", "proxypass");
        provider.Set("Http:ProxyDomain", "proxydomain");
        provider.Set("Http:Named:BaseAddress", "https://www.domain.com/");

        var configuration = new ConfigurationBuilder().Add(provider).Build();

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddHttpClient()
            .AddHttpClientConfiguration()
            .BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IHttpMessageHandlerFactory>();
        var handler = factory.CreateHandler("Named:Child").GetInnerHandlerOfType<HttpClientHandler>();

        handler.Proxy.Should().BeNull();
    }
}
