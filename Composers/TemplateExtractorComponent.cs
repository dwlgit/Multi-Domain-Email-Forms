using DigitalWonderlab.MultiDomainEmail.Services;
using Umbraco.Cms.Core.Composing;

namespace DigitalWonderlab.MultiDomainEmail.Composers
{
    public class TemplateExtractorComponent : IComponent
    {
        private readonly TemplateExtractor _templateExtractor;

        public TemplateExtractorComponent(TemplateExtractor templateExtractor)
        {
            _templateExtractor = templateExtractor;
        }

        public void Initialize()
        {
            _templateExtractor.ExtractDefaultTemplates();
        }

        public void Terminate()
        {
            // Nothing to clean up
        }
    }
}