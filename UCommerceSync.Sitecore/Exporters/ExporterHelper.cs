using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace UCommerceSync.Sitecore.Exporters
{
    public class ExporterHelper
    {
        public static Version GetUCommerceVersionFrom(string filePath)
        {
            Version result;
            if (!Version.TryParse(GetAttributeValueFrom(filePath, "version"), out result))
                throw new ArgumentException(string.Format("Could not parse export version from file: {0}", filePath));
            return result;
        }

        public static DateTime GetTimestampFrom(string filePath)
        {
            DateTime result;
            if (!DateTime.TryParse(GetAttributeValueFrom(filePath, "timestamp"), out result))
                throw new ArgumentException(string.Format("Could not parse export timestamp from file: {0}", filePath));
            return result;
        }

        private static string GetAttributeValueFrom(string filePath, string attributeName)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                throw new ArgumentException(string.Format("Could not find the file: {0}", filePath));
            if (fileInfo.Extension.ToLowerInvariant() != ".xml")
                throw new ArgumentException(string.Format("Cannot load export version from file: {0}", filePath));
            try
            {
                var xattribute = XDocument.Load(fileInfo.FullName).Root.Attributes(attributeName).FirstOrDefault();
                if (xattribute == null)
                    throw new ArgumentException(string.Format("Could not find attribute: {0} in file: {1}",
                        attributeName, filePath));
                return xattribute.Value;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(string.Format("Could not parse XML file: {0}", filePath), ex);
            }
        }
    }
}