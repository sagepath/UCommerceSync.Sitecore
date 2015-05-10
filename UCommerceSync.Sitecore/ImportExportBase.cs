using System;
using System.Collections.Generic;
using System.Linq;
using UCommerceSync.Logging;

namespace UCommerceSync.Sitecore
{
    public class ImportExportBase : UCommerceSync.ImportExportBase
    {
        protected ImportExportBase(string syncDataPath, ILogger log) : base(syncDataPath, log)
        {
        }

        protected static bool IsCollection(Type propertyType)
        {
            if (propertyType.IsGenericType)
                return propertyType.GetInterfaces().Any(t =>
                {
                    if (t.IsGenericType)
                        return t.GetGenericTypeDefinition() == typeof (IEnumerable<>);
                    return false;
                });
            return false;
        }

        protected static bool IsComplexType(Type propertyType)
        {
            return propertyType.AssemblyQualifiedName.ToLowerInvariant().Contains("ucommerce");
        }

    }
}