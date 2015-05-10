using UCommerceSync.Sitecore.Exporters;
using UCommerceSync.VersionSpecific;

namespace UCommerceSync.Sitecore.VersionSpecific
{
    public class VersionSpecificExporter : IVersionSpecificExporter
    {
        protected IExporter _exporter;

        public VersionSpecificExporter(IExporter exporter)
        {
            _exporter = exporter;
        }

        public virtual void ExportMarketingFoundationTargets()
        {            
        }

        public virtual void ExportMarketingFoundationAwards()
        {            
        }
    }
}