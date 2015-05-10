using UCommerce.EntitiesV2;

namespace UCommerceSync.Sitecore.Importers
{
    internal class AwardImporter : TargetAwardImporter<Award>
    {
        public AwardImporter(IImporter importer)
            : base(importer)
        {
        }

        protected override string GetGroupingValue(Award entity)
        {
            return string.Format("{0}>{1}", entity.CampaignItem.Campaign.Name, entity.CampaignItem.Name);
        }

        protected override object GetParent(Award entity)
        {
            return entity.CampaignItem;
        }
    }
}