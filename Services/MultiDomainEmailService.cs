using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Text;
using DigitalWonderlab.MultiDomainEmail.Models;

namespace DigitalWonderlab.MultiDomainEmail.Services
{
    public class MultiDomainEmailService : IMultiDomainEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<MultiDomainEmailService> _logger;

        public MultiDomainEmailService(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<MultiDomainEmailService> logger)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody, string textBody = null)
        {
            var currentDomain = GetCurrentDomain();
            await SendEmailAsync(to, subject, htmlBody, textBody, currentDomain);
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody, string textBody = null, string fromDomain = null)
        {
            try
            {
                var domain = fromDomain ?? GetCurrentDomain();
                var smtpConfig = GetSmtpConfigurationForDomain(domain);

                _logger.LogInformation("Sending email to {To} using SMTP config for domain {Domain}", to, domain);

                using var client = CreateSmtpClient(smtpConfig);
                using var mailMessage = CreateMailMessage(smtpConfig, to, subject, htmlBody, textBody);

                await client.SendMailAsync(mailMessage);

                _logger.LogInformation("Email sent successfully to {To} from {From}", to, smtpConfig.From);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw;
            }
        }

        private string GetCurrentDomain()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                var host = httpContext.Request.Host.Host.ToLowerInvariant();

                // Remove www. if present
                if (host.StartsWith("www."))
                {
                    host = host.Substring(4);
                }

                _logger.LogDebug("Current domain detected as: {Domain}", host);
                return host;
            }

            _logger.LogWarning("Could not detect current domain from HttpContext, falling back to default");
            return "default";
        }

        private SmtpConfiguration GetSmtpConfigurationForDomain(string domain)
        {
            // Try to get domain-specific configuration
            var domainConfig = _configuration.GetSection($"MultiDomainSmtpSettings:{domain}");

            if (domainConfig.Exists())
            {
                var config = new SmtpConfiguration();
                domainConfig.Bind(config);
                _logger.LogDebug("Using domain-specific SMTP config for {Domain}", domain);
                return config;
            }

            // Fallback to default configuration
            var defaultConfig = _configuration.GetSection("MultiDomainSmtpSettings:default");
            if (defaultConfig.Exists())
            {
                var config = new SmtpConfiguration();
                defaultConfig.Bind(config);
                _logger.LogDebug("Using default SMTP config for domain {Domain}", domain);
                return config;
            }

            // Final fallback to your existing SmtpSettings
            _logger.LogWarning("No multi-domain config found, falling back to legacy SmtpSettings");
            return new SmtpConfiguration
            {
                From = _configuration["SmtpSettings:From"] ?? "",
                Host = _configuration["SmtpSettings:Host"] ?? "",
                Port = int.Parse(_configuration["SmtpSettings:Port"] ?? "587"),
                Username = _configuration["SmtpSettings:Username"] ?? "",
                Password = _configuration["SmtpSettings:Password"] ?? ""
            };
        }

        private SmtpClient CreateSmtpClient(SmtpConfiguration config)
        {
            var client = new SmtpClient(config.Host, config.Port)
            {
                Credentials = new NetworkCredential(config.Username, config.Password),
                EnableSsl = config.SecureSocketOptions.Equals("Auto", StringComparison.OrdinalIgnoreCase) ||
                           config.SecureSocketOptions.Equals("StartTls", StringComparison.OrdinalIgnoreCase)
            };

            return client;
        }

        private MailMessage CreateMailMessage(SmtpConfiguration config, string to, string subject, string htmlBody, string textBody)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(config.From),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            // Add text version if provided
            if (!string.IsNullOrEmpty(textBody))
            {
                var textView = AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain");
                var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");

                mailMessage.AlternateViews.Add(textView);
                mailMessage.AlternateViews.Add(htmlView);
            }

            return mailMessage;
        }
    }
}