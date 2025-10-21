using DigitalWonderlab.MultiDomainEmail.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Forms.Core.Configuration;
using DigitalWonderlab.MultiDomainEmail.Workflows;

#if FORMS_15PLUS
using Umbraco.Forms.Core.Providers; // WorkflowCollectionBuilder lives here in Forms v15+
#endif

namespace DigitalWonderlab.MultiDomainEmail.Composers
{
    public class MultiDomainEmailComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddTransient<IMultiDomainEmailService, MultiDomainEmailService>();
            builder.Services.AddHttpContextAccessor();

#if FORMS_15PLUS
            // Forms v15+
            builder.WithCollectionBuilder<WorkflowCollectionBuilder>()
                   .Add<MultiDomainEmailWorkflow>();
#else
            // Forms v13–14
            builder.Services.AddSingleton<MultiDomainEmailWorkflow>();
#endif

            builder.Services.AddSingleton<IConfigureOptions<Recaptcha3Settings>, MultiDomainRecaptcha3ConfigureOptions>();
        }
    }
}
