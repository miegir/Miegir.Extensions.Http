# Miegir.Extensions.Http

A .net library for configuring `HttpClient` instances from `IConfiguration` sources such as the appsettings.json file through dependency injection.

## Getting Started

To start using the configuration for any `HttpClient`, simply invoke `AddHttpClientConfiguration` on your `IServiceCollection` instance. When invoked without arguments, the `AddHttpClientConfiguration` will use the section named `Http` from the global `IConfiguration` instance resolved from the container. You can configure this behavior by providing `configureAction` delegate.

### Examples

Basic usage:
```C#
var builder = Host.CreateApplicationBuilder();

// Add default IHttpClientFactory implementation.
builder.Services.AddHttpClient();

// Allow HttpClient instances to be configured.
builder.Services.AddHttpClientConfiguration();
```

We can specify the root configuration section name:
```C#
builder.Services.AddHttpClientConfiguration(options =>
{
    // "Http" is the default value for the GlobalConfigurationSectionPath.
    options.GlobalConfigurationSectionPath = "Http";
    // We can use entire global IConfiguration instance by specifying
    // null or empty string in the GlobalConfigurationSectionPath.
    options.GlobalConfigurationSectionPath = null;
});
```

We can also provide our own IConfiguration instance:
```C#
builder.Services.AddHttpClientConfiguration(options =>
{
    // The GlobalConfigurationSectionPath will be ignored
    // when we provide the explicit Configuration.
    options.Configuration = new ConfigurationBuilder()
        // ... add some providers here ...
        .Build();
});
```

We can configure our clients manually. The confugiration from `IConfiguration` instance will be provided on top of that (since it is applied with IPostConfigureOptions&lt;HttpClientFactgoryOptions>).
```C#
builder.Services.AddHttpClientConfiguration();

builder.Services.AddHttpClient<OurClientType>()
    .ConfigureHttpClient(httpClient =>
    {
        // This address can be overridden with
        //   "Http:OurClientType:BaseAddress" configuration key.
        // The "Http:BaseAddress" key will also be consulted when
        //   "Http:OurClientType:BaseAddress" is not specified.
        httpClient.BaseAddress = new Uri(
            "https://default.base.address/",
            UriKind.Absolute);
    });
```

## Supported properties

### HttpClient

The following properties of the HttpClient can be configured:
- BaseAddress.
- Timeout.
- DefaultRequestHeaders. Will be read from `Headers` section.

### HttpClientHandler

The following properties of the underlying HttpClientHandler can be configured:
- AllowAutoRedirect.
- Proxy. This is configured using `ProxyUrl` configuration option. If `ProxyUrl` is not empty, the `WebProxy` instance will be created with this url. The `WebProxy.Credentials` property is initialized to `NetworkCredential` instance with the following proepties read from options:
    - UserName. Will be read from `ProxyUserName`.
    - Password. Will be read from `ProxyPassword`.
    - Domain. Will be read from `ProxyDomain`.

## Configuration hierarchy

HttpClient configuration is organized hierarchically. Consider the following example from appsettings.json:

```json
{
    "Http": {
        "BaseAddress": "https://www.mydomain.com/mypath/",
        "Timeout": "0:00:20",
        "Parent": {
            "Timeout": "0:01:00",
            "Child": {
                "BaseAddress": "http://www.childaddress.com/"
            }
        }
    }
}
```

With this configuration, the unnamed `HttpClient` will have the `BaseAddress` equal to `"https://www.mydomain.com/mypath/"` and its `Timeout` will be 20 seconds. The `HttpClient` named `"Parent"` will have the same base address but its timeout will be one minute. The `HttpClient` with name `"Parent:Child"` will have the `BaseAddress` of `"http://www.childaddress.com/"`, ant its timeout will be one minute.

## Proxy hierarchy

The proxy for an `HttpClientHandler` will be inherited as long as `BaseAddress` is not overridden. When `BaseAddress` is overridden (even if the same as its parent), the proxy is no longer inherited and should be specified explicitly.

Consider the following confgiuration:
```json
{
    "Http": {
        "A": {
            "BaseAddress": "https://www.one.org/",
            "B": {
                "ProxyUrl": "http://myproxy/",
                "C": {
                    "BaseAddress": "https://www.two.org/",
                    "D": {
                        "ProxyUrl": "http://myproxy/"
                    }
                }
            }
        }
    }
}
```

Http clients named `"A"` and `"A:B:C"` will not have a proxy, but clients named `"A:B"`, `"A:B:C:D"` and `"A:B:C:D:anything"` will.

## Additional validation

When using `AddHttpClientConfiguration`, every `HttpClient` created with `IHttpClientFactory` will require its `BaseAddress` to be specified. An exception will be raised if `BaseAddress` will be null. You can provide default `BaseAddress` in code (by using `ConfigureHttpClient` for example).

When `BaseAddress` is applied from `IConfiguration` instance, the following rules are checked:
- The address should be absolute. Relative `Uri` cannot be assigned to `HttpClient.BaseAddress` anyway.
- `AbsolutePath` of the address should end with `'/'`. This ensures that confiured address will be used exactly as specified when making requests. For example, if the base address would be specified as `"https://domain/root/path"` (not ending with `'/'`) then when performing `httpClient.GetAsync("address")` the request will be made to `"https://domain/root/address"` and not to `"https://domain/root/path/address"` as one might expect. We therefore prohibit paths not ending with `'/'`. This rule is not enforced when `HttpClient` is configured in code.

## DefaultRequestHeaders

You can configure headers as plain values as well as arrays.
```jsonc
{
    "Http": {
        "Client": {
            "Headers": {
                "Header1": "value1",
                "Header2": ["a", "b", "c"]
            },
            "Inherited": {
                "Headers": {
                    // inherit Header1 but not Header2
                    "Header2": null
                }
            }
        }
    }
}
```
