using System.Collections.Generic;
using UCommerce.EntitiesV2;

namespace UCommerceSync.Sitecore.Exporters
{
    public interface IExporter
    {
        void Export();
        IEnumerable<T> ExportableTargets<T>(IEnumerable<T> targets) where T : Target;
        void ExportEntitites<T>(IEnumerable<T> entities) where T : IEntity;        
    }
}