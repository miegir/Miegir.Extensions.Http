using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to configure an <see cref="IServiceCollection"/> for <see cref="IHttpClientFactory"/>.
    /// </summary>
    public static class HttpClientConfigurationExtensions
    {
        /// <summary>
        /// Adds the services required to configure <see cref="IHttpClientFactory"/> from the <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddHttpClientConfiguration(this IServiceCollection services)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<
                    IPostConfigureOptions<HttpClientFactoryOptions>,
                    HttpClientConfigurationHandler>());

            return services;
        }

        /// <summary>
        /// Adds the services required to configure <see cref="IHttpClientFactory"/> from the <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configureAction">A delegate that is used to adjust the <see cref="HttpClientConfigurationOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddHttpClientConfiguration(this IServiceCollection services, Action<HttpClientConfigurationOptions> configureAction)
        {
            return services.AddHttpClientConfiguration().Configure(configureAction);
        }
    }
}
