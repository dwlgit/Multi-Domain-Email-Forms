using DigitalWonderlab.MultiDomainEmail.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Forms.Core.Configuration;
using Umbraco.Forms.Core.Providers;
using DigitalWonderlab.MultiDomainEmail.Workflows;

namespace DigitalWonderlab.MultiDomainEmail.Composers
{
    public class MultiDomainEmailComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddTransient<IMultiDomainEmailService, MultiDomainEmailService>();
            builder.Services.AddHttpContextAccessor();

            // Works for all Forms versions (v13-16+)
            builder.WithCollectionBuilder<WorkflowCollectionBuilder>()
                   .Add<MultiDomainEmailWorkflow>();

            builder.Services.AddSingleton<IConfigureOptions<Recaptcha3Settings>, MultiDomainRecaptcha3ConfigureOptions>();

            // Register the template extractor
            builder.Services.AddSingleton<TemplateExtractor>();

            // Register component to extract templates on startup
            builder.Components().Append<TemplateExtractorComponent>();
        }
    }
}