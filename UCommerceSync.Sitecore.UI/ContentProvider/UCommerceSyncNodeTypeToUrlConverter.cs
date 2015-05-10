using UCommerce.Tree;

namespace UCommerceSync.Sitecore.UI.ContentProvider
{
    public class UCommerceSyncNodeTypeToUrlConverter : ITreeNodeTypeToUrlConverter
    {
        public bool TryConvert(string type, string itemId, string itemContextId, out string url)
        {
            url = string.Empty;
            switch (type)
            {
                case Constants.UcommerceSync:
                    url = Constants.UCommerceSyncUrls.UCommerceSyncBaseUrl;
                    break;
                case Constants.UcommerceSyncExport:
                    url = Constants.UCommerceSyncUrls.UCommerceSyncExportUrl;
                    break;
                case Constants.UcommerceSyncImport:
                    url = Constants.UCommerceSyncUrls.UCommerceSyncImportUrl;
                    break;
                default:
                    return false;
            }

            return true;
        }
    }
}