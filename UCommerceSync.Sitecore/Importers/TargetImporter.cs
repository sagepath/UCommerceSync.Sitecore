using UCommerce.EntitiesV2;

namespace UCommerceSync.Sitecore.Importers
{
    internal class TargetImporter : TargetAwardImporter<Target>
    {
        public TargetImporter(IImporter importer)
            : base(importer)
        {
        }

        protected override string GetGroupingValue(Target entity)
        {
            return string.Format("{0}>{1}", entity.CampaignItem.Campaign.Name, entity.CampaignItem.Name);
        }

        protected override object GetParent(Target entity)
        {
            return entity.CampaignItem;
        }
    }
}