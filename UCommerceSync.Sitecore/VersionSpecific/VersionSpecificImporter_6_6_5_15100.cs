using UCommerce.EntitiesV2;

namespace UCommerceSync.Sitecore.VersionSpecific
{
    public class VersionSpecificImporter_6_6_5_15100 : VersionSpecificImporter
    {
        public VersionSpecificImporter_6_6_5_15100(Importers.IImporter importer)
            : base(importer)
        {
        }

        public override void ImportMarketingFoundationTargets(EntityReferenceResolver resolver)
        {
            Importer.ImportTargets<PriceGroupTarget>(resolver);
        }
    }
}