using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Http
{
    /// <summary>
    /// Configures <see cref="IHttpClientFactory"/> from the <see cref="HttpClientConfigurationOptions"/>.
    /// </summary>
    public class HttpClientConfigurationHandler : IPostConfigureOptions<HttpClientFactoryOptions>
    {
        private readonly OptionsCache<Option<HttpClientConfigurationInstance>> cache = new OptionsCache<Option<HttpClientConfigurationInstance>>();
        private readonly IDisposable? monitorRegistration;
        private HttpClientConfigurationSource source;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="monitor">Monitor for the <see cref="HttpClientConfigurationOptions"/>.</param>
        /// <param name="globalConfigurationInstance">The <see cref="IConfiguration"/> instance that will be used when
        /// no explicit <see cref="IConfiguration"/> instance is provided in <see cref="HttpClientConfigurationOptions"/>.</param>
        public HttpClientConfigurationHandler(
            IOptionsMonitor<HttpClientConfigurationOptions> monitor,
            IConfiguration? globalConfigurationInstance = null)
        {
            source = new HttpClientConfigurationSource(globalConfigurationInstance);
            monitorRegistration = monitor.OnChange(HandleOptions);

            HandleOptions(monitor.CurrentValue);

            void HandleOptions(HttpClientConfigurationOptions options)
            {
                var oldRegistration = source;

                var newConfiguration = options.Configuration
                    ?? (string.IsNullOrEmpty(options.GlobalConfigurationSectionName)
                    ? globalConfigurationInstance
                    : globalConfigurationInstance?.GetSection(options.GlobalConfigurationSectionName));

                var newSubscription = newConfiguration != null
                    ? ChangeToken.OnChange(
                        newConfiguration.GetReloadToken,
                        cache => cache.Clear(),
                        cache)
                    : null;

                var newRegistration = new HttpClientConfigurationSource(
                    newConfiguration, newSubscription);

                if (Interlocked.CompareExchange(
                    ref source, newRegistration, oldRegistration) != oldRegistration)
                {
                    newRegistration.Dispose();
                }
            }
        }

        /// <summary>
        /// Removes all change registration subscriptions.
        /// </summary>
        public void Dispose()
        {
            monitorRegistration?.Dispose();
            source.Dispose();
        }

        /// <inheritdoc/>
        public void PostConfigure(string? name, HttpClientFactoryOptions options)
        {
            name ??= string.Empty;

            options.HttpClientActions.Add(client =>
            {
                var instance = GetInstance();

                if (instance == null)
                {
                    return;
                }

                if (instance.BaseAddress != null)
                {
                    client.BaseAddress = instance.BaseAddress;
                }

                if (client.BaseAddress is null)
                {
                    instance.ThowBaseAddressShouldNotBeNull();
                }

                if (instance.Timeout != null)
                {
                    client.Timeout = instance.Timeout.Value;
                }

                foreach (var (name, value) in instance.Headers)
                {
                    if (value != null)
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation(name, value);
                    }
                }
            });

            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                if (builder.PrimaryHandler is HttpClientHandler handler)
                {
                    var instance = GetInstance();

                    if (instance == null)
                    {
                        return;
                    }

                    if (instance.AllowAutoRedirect.HasValue)
                    {
                        handler.AllowAutoRedirect = instance.AllowAutoRedirect.Value;
                    }

                    if (handler.Proxy is null)
                    {
                        if (instance.ProxyUrl != null)
                        {
                            handler.Proxy = new WebProxy(instance.ProxyUrl)
                            {
                                Credentials = new NetworkCredential(
                                    instance.ProxyUserName,
                                    instance.ProxyPassword,
                                    instance.ProxyDomain),
                            };
                        }
                    }
                }
            });

            HttpClientConfigurationInstance? GetInstance() => cache.GetOrAdd(name, () =>
            {
                var configuration = source.Configuration;
                if (configuration == null)
                {
                    return new Option<HttpClientConfigurationInstance>();
                }

                var instance = new HttpClientConfigurationInstance(configuration, name);

                instance.ApplyConfiguration(configuration);

                if (!string.IsNullOrEmpty(name))
                {
                    var segments = name.Split(':');

                    foreach (var segment in segments)
                    {
                        configuration = configuration.GetSection(segment);
                        instance.ApplyConfiguration(configuration);
                    }
                }

                return new Option<HttpClientConfigurationInstance>(instance);
            }).Value;
        }

        private class Option<T> where T : class
        {
            public Option() { }
            public Option(T? value) => Value = value;
            public T? Value { get; }
        }
    }
}
