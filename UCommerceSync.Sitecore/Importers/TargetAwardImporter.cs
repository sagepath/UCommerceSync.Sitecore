using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UCommerceSync.Sitecore.Importers
{
    internal abstract class TargetAwardImporter<TB>
    {
        private readonly IImporter _importer;

        protected TargetAwardImporter(IImporter importer)
        {
            _importer = importer;
        }

        protected abstract string GetGroupingValue(TB entity);
        protected abstract object GetParent(TB entity);

        public List<T> ImportEntities<T>(EntityReferenceResolver resolver) where T : class, TB, new()
        {
            var entityType = typeof (T);
            var all = _importer.GetAll(entityType);
            if (all == null)
                throw new ArgumentException("Could not find method All() on type " + entityType.Name);
            var list1 =
                (all.Invoke(null, null) as IEnumerable).OfType<T>().ToList();
            var list2 = new List<T>();
            var repository = _importer.GetRepository<T>();
            foreach (
                var keyValuePair in
                    _importer.LoadImportEntities(entityType).Select(e =>
                        _importer.ParseEntity(entityType, e, resolver)
                            as T).GroupBy(GetGroupingValue).ToDictionary(g => g.Key, g => g.ToList()))
            {
                var groupingValue = keyValuePair.Key;
                var importEntitiesBySignature =
                    keyValuePair.Value.GroupBy(_importer.GetEntitySignature).ToDictionary(g => g.Key, g => g.ToList());
                var list3 = list1.Where(t =>
                {
                    if (GetGroupingValue(t) == groupingValue)
                        return !importEntitiesBySignature.ContainsKey(_importer.GetEntitySignature(t));
                    return false;
                }).ToList();
                if (list3.Any())
                {
                    repository.Delete(list3);
                    list1.RemoveAll(list3.Contains);
                }
                foreach (var str in importEntitiesBySignature.Keys)
                {
                    var signature = str;
                    var list4 =
                        list1.Where(t =>
                        {
                            if (GetGroupingValue(t) == groupingValue)
                                return _importer.GetEntitySignature(t) == signature;
                            return false;
                        }).ToList();
                    var list5 = importEntitiesBySignature[signature];
                    if (list4.Count < list5.Count)
                    {
                        var num = list5.Count - list4.Count;
                        for (var index = 0; index < num; ++index)
                            list4.Add(Activator.CreateInstance<T>());
                    }
                    else if (list4.Count > list5.Count)
                    {
                        var range = list4.GetRange(0, list4.Count - list5.Count);
                        list4.RemoveRange(0, list4.Count - list5.Count);
                        repository.Delete(range);
                    }
                    for (var index = 0; index < list5.Count; ++index)
                        _importer.SynchronizeEntities(list5[index], list4[index],
                            resolver, GetParent(list5[index]));
                    list2.AddRange(list4);
                    repository.Save(list4);
                }
            }
            return list2;
        }
    }
}