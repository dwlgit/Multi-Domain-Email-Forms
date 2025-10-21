using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Umbraco.Forms.Core.Configuration;

namespace DigitalWonderlab.MultiDomainEmail.Services
{
    public class MultiDomainRecaptcha3ConfigureOptions : IConfigureOptions<Recaptcha3Settings>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public MultiDomainRecaptcha3ConfigureOptions(
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public void Configure(Recaptcha3Settings options)
        {
            var host = _httpContextAccessor.HttpContext?.Request?.Host.Host?.ToLowerInvariant() ?? "default";

            if (host.StartsWith("www."))
            {
                host = host[4..];
            }

            var section = _configuration.GetSection($"MultiDomainRecaptcha:{host}");

            if (!section.Exists())
            {
                section = _configuration.GetSection("MultiDomainRecaptcha:default");
            }

            if (section.Exists())
            {
                options.SiteKey = section["SiteKey"] ?? string.Empty;
                options.PrivateKey = section["PrivateKey"] ?? string.Empty;
            }
        }
    }
}