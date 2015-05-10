using UCommerceSync.VersionSpecific;

namespace UCommerceSync.Sitecore.VersionSpecific
{
    public abstract class VersionSpecificImporter : IVersionSpecificImporter
    {
        protected Importers.IImporter Importer;

        protected VersionSpecificImporter(Importers.IImporter importer)
        {
            Importer = importer;
        }

        public virtual void ImportMarketingFoundationTargets(EntityReferenceResolver resolver)
        {
        }

        public virtual void ImportMarketingFoundationAwards(EntityReferenceResolver resolver)
        {
        }
    }
}