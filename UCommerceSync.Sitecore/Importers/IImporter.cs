using System.Collections.Generic;
using UCommerce.EntitiesV2;

namespace UCommerceSync.Sitecore.Importers
{
    public interface IImporter : UCommerceSync.IImporter
    {
        List<T> ImportTargets<T>(EntityReferenceResolver resolver) where T : Target, new();
    }
}