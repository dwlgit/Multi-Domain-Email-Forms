using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Hosting;

namespace DigitalWonderlab.MultiDomainEmail.Services
{
    public class TemplateExtractor
    {
        private readonly ILogger<TemplateExtractor> _logger;
        private readonly IHostingEnvironment _hostingEnvironment;

        public TemplateExtractor(
            ILogger<TemplateExtractor> logger,
            IHostingEnvironment hostingEnvironment)
        {
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
        }

        public void ExtractDefaultTemplates()
        {
            try
            {
                // Log all embedded resources
                var assembly = typeof(TemplateExtractor).Assembly;
                var resources = assembly.GetManifestResourceNames();

                _logger.LogInformation("Found {Count} embedded resources:", resources.Length);
                foreach (var resource in resources)
                {
                    _logger.LogInformation("  - {ResourceName}", resource);
                }

                var webRootPath = _hostingEnvironment.MapPathContentRoot("~");
                var targetPath = Path.Combine(webRootPath, "Views", "Partials", "Forms", "EmailTemplates");

                _logger.LogInformation("Target extraction path: {Path}", targetPath);

                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                    _logger.LogInformation("Created directory: {Path}", targetPath);
                }

                ExtractTemplate(targetPath, "AdminNotification.cshtml");
                ExtractTemplate(targetPath, "SubmitterConfirmation.cshtml");

                ExtractReadme(targetPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract email templates");
            }
        }

        private void ExtractTemplate(string targetPath, string fileName)
        {
            var filePath = Path.Combine(targetPath, fileName);

            _logger.LogInformation("Attempting to extract template to: {FilePath}", filePath);

            if (File.Exists(filePath))
            {
                _logger.LogInformation("Template already exists, skipping: {FileName}", fileName);
                return;
            }

            var assembly = typeof(TemplateExtractor).Assembly;
            var resourceName = $"DigitalWonderlab.MultiDomainEmail.Templates.{fileName}";

            _logger.LogInformation("Looking for embedded resource: {ResourceName}", resourceName);

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogWarning("Could not find embedded resource: {ResourceName}", resourceName);
                return;
            }

            using var fileStream = File.Create(filePath);
            stream.CopyTo(fileStream);

            _logger.LogInformation("Successfully extracted template: {FileName} to {FilePath}", fileName, filePath);
        }

        private void ExtractReadme(string targetPath)
        {
            var readmePath = Path.Combine(targetPath, "README.txt");

            if (File.Exists(readmePath))
                return;

            var readmeContent = @"Multi-Domain Email Templates
============================

These templates are used by the Multi-Domain Email workflow for Umbraco Forms.

Customization:
- Edit these .cshtml files to customize email appearance
- Changes will be preserved when updating the package
- If you delete a template, it will be recreated on next startup

Templates:
- AdminNotification.cshtml - Email sent to admin/specified recipient
- SubmitterConfirmation.cshtml - Thank you email sent to form submitter

For more information, visit:
https://github.com/digitalwonderlab/umbraco-multidomain-email
";

            File.WriteAllText(readmePath, readmeContent);
            _logger.LogInformation("Created README in templates folder");
        }
    }
}