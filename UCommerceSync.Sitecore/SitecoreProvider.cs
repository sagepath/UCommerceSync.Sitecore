using System;
using UCommerceSync.Cms;

namespace UCommerceSync.Sitecore
{
    public class SitecoreProvider : ICmsProvider
    {
        public string MediaIdToIdentifier(string mediaId)
        {
            //throw new NotImplementedException();
            return mediaId;
        }

        public string IdentifierToMediaId(string identifier)
        {
            //throw new NotImplementedException();
            return identifier;
        }

        public string ContentIdToIdentifier(string contentId)
        {
            //throw new NotImplementedException();
            return contentId;
        }

        public string IdentifierToContentId(string identifier)
        {
            //throw new NotImplementedException();
            return identifier;            
        }
    }
}