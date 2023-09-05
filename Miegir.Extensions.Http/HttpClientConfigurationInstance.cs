using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http
{
    internal class HttpClientConfigurationInstance
    {
        private readonly string clientName;

        public HttpClientConfigurationInstance(IConfiguration configuration, string clientName)
        {
            Configuration = configuration;
            this.clientName = clientName;
        }

        public Uri? BaseAddress { get; set; }
        public TimeSpan? Timeout { get; set; }
        public bool? AllowAutoRedirect { get; set; }
        public Dictionary<string, string?[]?> Headers { get; } = new Dictionary<string, string?[]?>();
        public string? ProxyUrl { get; set; }
        public string? ProxyUserName { get; set; }
        public string? ProxyPassword { get; set; }
        public string? ProxyDomain { get; set; }
        public IConfiguration Configuration { get; private set; }

        private string DisplayName => string.IsNullOrEmpty(clientName) ? "HttpClient" : $"HttpClient[{clientName}]";

        public void ApplyConfiguration(IConfiguration configuration)
        {
            Configuration = configuration;

            var baseAddress = GetAbsoluteUri(nameof(BaseAddress));
            if (baseAddress != null)
            {
                BaseAddress = baseAddress;
            }

            var timeout = GetTimeSpan(nameof(Timeout));
            if (timeout != null)
            {
                Timeout = timeout;
            }

            var allowAutoRedirects = GetBool(nameof(AllowAutoRedirect));
            if (allowAutoRedirects != null)
            {
                AllowAutoRedirect = allowAutoRedirects;
            }

            var headers = configuration.GetSection(nameof(Headers));
            foreach (var header in headers.GetChildren())
            {
                Headers[header.Key] = GetHeaderValue(header);
            }

            var proxyUrl = configuration[nameof(ProxyUrl)];
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                ProxyUrl = proxyUrl;
                ProxyUserName = configuration[nameof(ProxyUserName)];
                ProxyPassword = configuration[nameof(ProxyPassword)];
                ProxyDomain = configuration[nameof(ProxyDomain)];
            }
            else if (baseAddress != null)
            {
                ProxyUrl = null;
                ProxyUserName = null;
                ProxyPassword = null;
                ProxyDomain = null;
            }
        }

        public void ThrowBaseAddressShouldNotBeNull()
        {
            var message = $"{DisplayName}: {nameof(BaseAddress)} should not be null.";
            throw new OptionsValidationException(
                Configuration.GetSection(nameof(BaseAddress)).Path,
                typeof(Uri),
                new[] { message });
        }

        private static string?[]? GetHeaderValue(IConfigurationSection header)
        {
            if (header.Value != null)
            {
                return new[] { header.Value };
            }

            List<string?>? builder = null;

            foreach (var child in header.GetChildren())
            {
                builder ??= new List<string?>();
                builder.Add(child.Value);
            }

            return builder?.ToArray();
        }

        private Uri? GetAbsoluteUri(string sectionName)
        {
            var section = Configuration.GetSection(sectionName);

            // cache the value so it is requested only once
            var s = section.Value;

            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            try
            {
                var uri = new Uri(s, UriKind.RelativeOrAbsolute);

                if (!uri.IsAbsoluteUri)
                {
                    var message = $"{DisplayName}: {sectionName} should be absolute.";
                    throw new OptionsValidationException(
                        section.Path, typeof(Uri), new[] { message });
                }

                if (!uri.AbsolutePath.EndsWith('/'))
                {
                    var message = $"{DisplayName}: {sectionName} should end with '/'.";
                    throw new OptionsValidationException(
                        section.Path, typeof(Uri), new[] { message });
                }

                return uri;
            }
            catch (UriFormatException ex)
            {
                var message = $"{DisplayName}: {sectionName} is invalid. {ex.Message}";
                throw new OptionsValidationException(
                    section.Path, typeof(Uri), new[] { message });
            }
        }

        private TimeSpan? GetTimeSpan(string sectionName)
        {
            var section = Configuration.GetSection(sectionName);

            // cache the value so it is requested only once
            var s = section.Value;

            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            try
            {
                return TimeSpan.Parse(s);
            }
            catch (FormatException ex)
            {
                var message = $"{DisplayName}: {sectionName} is invalid. {ex.Message}";
                throw new OptionsValidationException(
                    section.Path, typeof(TimeSpan), new[] { message });
            }
        }

        private bool? GetBool(string sectionName)
        {
            var section = Configuration.GetSection(sectionName);

            // cache the value so it is requested only once
            var s = section.Value;

            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            try
            {
                return bool.Parse(s);
            }
            catch (FormatException ex)
            {
                var message = $"{DisplayName}: {sectionName} is invalid. {ex.Message}";
                throw new OptionsValidationException(
                    section.Path, typeof(bool), new[] { message });
            }
        }
    }
}
