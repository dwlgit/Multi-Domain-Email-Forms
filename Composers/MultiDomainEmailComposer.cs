using DigitalWonderlab.MultiDomainEmail.Services;
using DigitalWonderlab.MultiDomainEmail.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Forms.Core.Configuration;
using Umbraco.Forms.Core.Providers;

namespace DigitalWonderlab.MultiDomainEmail.Composers
{
    public class MultiDomainEmailComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddTransient<IMultiDomainEmailService, MultiDomainEmailService>();

            builder.Services.AddHttpContextAccessor();

            builder.WithCollectionBuilder<WorkflowCollectionBuilder>()
                .Add<MultiDomainEmailWorkflow>();

            builder.Services.AddSingleton<IConfigureOptions<Recaptcha3Settings>, MultiDomainRecaptcha3ConfigureOptions>();
        }
    }
}