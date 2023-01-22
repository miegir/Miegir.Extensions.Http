using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Http
{
    internal class HttpClientConfigurationSource : IDisposable
    {
        private readonly IDisposable? registration;

        public HttpClientConfigurationSource(IConfiguration? configuration, IDisposable? registration = null)
            => (Configuration, this.registration) = (configuration, registration);

        public IConfiguration? Configuration { get; }

        public void Dispose() => registration?.Dispose();
    }
}
