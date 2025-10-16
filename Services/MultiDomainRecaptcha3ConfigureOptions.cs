using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Umbraco.Forms.Core.Configuration;

public class MultiDomainRecaptcha3ConfigureOptions : IConfigureOptions<Recaptcha3Settings>
{
    private readonly IHttpContextAccessor _http;
    private readonly IConfiguration _config;

    public MultiDomainRecaptcha3ConfigureOptions(IHttpContextAccessor http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public void Configure(Recaptcha3Settings options)
    {
        var host = _http.HttpContext?.Request?.Host.Host?.ToLowerInvariant() ?? "default";
        if (host.StartsWith("www.")) host = host.Substring(4);

        var section = _config.GetSection($"MultiDomainRecaptcha:{host}");
        if (!section.Exists())
        {
            section = _config.GetSection("MultiDomainRecaptcha:default");
        }

        options.SiteKey = section["SiteKey"];
        options.PrivateKey = section["PrivateKey"];
    }
}
