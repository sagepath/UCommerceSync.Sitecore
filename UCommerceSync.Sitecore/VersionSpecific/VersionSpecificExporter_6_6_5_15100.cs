using System.Linq;
using UCommerce.EntitiesV2;
using UCommerceSync.Sitecore.Exporters;
using UCommerceSync.VersionSpecific;

namespace UCommerceSync.Sitecore.VersionSpecific
{
    public class VersionSpecificExporter_6_6_5_15100 : VersionSpecificExporter, IVersionSpecificExporter
    {
        public VersionSpecificExporter_6_6_5_15100(IExporter exporter) : base(exporter)
        {
        }

        public override void ExportMarketingFoundationTargets()
        {
            _exporter.ExportEntitites(
                _exporter.ExportableTargets(
                    PriceGroupTarget.All().ToList()));
        }
    }
}