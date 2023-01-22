using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Http
{
    /// <summary>
    /// An options class for configuring the <see cref="IHttpClientFactory"/> confuguration.
    /// </summary>
    public class HttpClientConfigurationOptions
    {
        /// <summary>
        /// Gets or sets the <see cref="IConfiguration"/> instance that will be used to configure an <see cref="IHttpClientFactory"/>.
        /// </summary>
        /// <remarks>
        /// <para>When this property is not <c>null</c>, the supplied configuration instance is used
        /// and the <see cref="GlobalConfigurationSectionName"/> property is ignored.</para>
        /// <para>When this property is <c>null</c>, the section of the global <see cref="IConfiguration"/>
        /// instance registered in the service provider is used. The section name is specified in the
        /// <see cref="GlobalConfigurationSectionName"/> property and defaults to <c>Http</c>.</para>
        /// </remarks>
        public IConfiguration? Configuration { get; set; }

        /// <summary>
        /// Gets or sets a section name in the global <see cref="IConfiguration"/> instance that will be used
        /// to configure an <see cref="IHttpClientFactory"/>. Default value is <c>Http</c>. You can specify 
        /// <c>null</c> or an empty string to use the entire <see cref="IConfiguration"/> instance.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is not used when explicit  <see cref="IConfiguration"/> instance is specified 
        /// in the <see cref="Configuration"/> property.
        /// </para>
        /// </remarks>
        public string? GlobalConfigurationSectionName { get; set; } = "Http";
    }
}
