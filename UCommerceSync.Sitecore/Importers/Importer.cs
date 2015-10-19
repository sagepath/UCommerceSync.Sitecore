using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UCommerce.EntitiesV2;
using UCommerce.Infrastructure;
using UCommerce.Security;
using UCommerceSync.Logging;
using UCommerceSync.Sitecore.VersionSpecific;
using UCommerceSync.VersionSpecific;

namespace UCommerceSync.Sitecore.Importers
{
    public class Importer : ImportExportBase, IImporter
    {
        private readonly ImportSettings _settings;
        public static BeforeImportHandler OnBeforeImport;
        public static AfterImportHandler OnAfterImport;
        private readonly IVersionSpecificImporter _versionImporter;

        public Importer(ImportSettings settings, string syncDataPath, ILogger log)
            : base(syncDataPath, log)
        {
            _settings = settings;
            if (!_settings.ImportProductCatalogGroups)
                _settings.ImportProductCatalogs = false;
            if (!_settings.ImportProductCatalogs)
                _settings.ImportProductCategories = false;
            if (!_settings.ImportProductCategories)
                _settings.ImportProducts = false;
            if (!(CurrentUCommerceVersion() >= PriceGroupTargetVersion))
                return;
            _versionImporter = new VersionSpecificImporter_6_6_5_15100(this);
        }

        public void Import()
        {
            if (_settings.ImportMarketingFoundation && this.GetEntitySignature((object)new VoucherTarget()).ToLowerInvariant() != "vouchertarget|false|false")
            {
                this.Log.Error("Voucher target entity type contains unexpected properties - aborting import for security reasons");
            }
            else
            {
                BeforeImportHandler beforeImportHandler = OnBeforeImport;
                if (beforeImportHandler != null)
                {
                    Log.Info("## Invoking OnBeforeImport event handler ##");
                    try
                    {
                        if (!beforeImportHandler(SyncDataPath, Log))
                        {
                            Log.Error("OnBeforeImport event handler returned false - aborting import");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "OnBeforeImport event handler threw an exception - aborting import", new object[0]);
                        return;
                    }
                    Log.Info("OnBeforeImport event handler invoked");
                }
                EntityReferenceResolver resolver = new EntityReferenceResolver(this);
                this.Import<DataType>(resolver);
                this.Import<DefinitionType>(resolver);
                this.Import<ProductDefinition>(resolver);
                this.Import<Currency>(resolver);
                this.Import<Country>(resolver);
                this.Import<OrderNumberSerie>(resolver);
                this.Import<EmailType>(resolver);
                this.Import<EmailProfile>(resolver);
                this.Import<ProductRelationType>(resolver);
                this.Import<PriceGroup>(resolver);
                if (this._settings.ImportProductCatalogGroups)
                    this.Import<ProductCatalogGroup>(resolver);
                if (this._settings.ImportProductCatalogs)
                    this.Import<ProductCatalog>(resolver);
                if (this._settings.ImportProductCategories)
                    this.Import<Category>(resolver);
                List<Product> list = (List<Product>)null;
                if (this._settings.ImportProducts)
                    list = this.ImportProducts();
                this.Import<PaymentMethod>(resolver);
                this.Import<ShippingMethod>(resolver);
                if (this._settings.PerformCleanUp)
                {
                    this.CleanUp<ShippingMethod>(resolver);
                    this.CleanUp<PaymentMethod>(resolver);
                    if (this._settings.ImportProductCatalogGroups)
                        this.CleanUp<ProductCatalogGroup>(resolver, ExistingProductCatalogGroups());
                    if (this._settings.ImportProductCatalogs)
                        this.CleanUp<ProductCatalog>(resolver, ExistingProductCatalogs());
                    if (this._settings.ImportProductCategories)
                        this.CleanUp<Category>(resolver, ExistingCategories());
                    if (this._settings.ImportProducts && list != null)
                        this.CleanUp<Product>(resolver, ExistingProducts(), Enumerable.ToList<object>(Enumerable.OfType<object>((IEnumerable)list)));
                    this.CleanUp<PriceGroup>(resolver);
                    this.CleanUp<ProductRelationType>(resolver);
                    this.CleanUp<EmailProfile>(resolver);
                    this.CleanUp<EmailType>(resolver);
                    this.CleanUp<OrderNumberSerie>(resolver);
                    this.CleanUp<Country>(resolver);
                    this.CleanUp<Currency>(resolver);
                    this.CleanUp<ProductDefinition>(resolver);
                    this.CleanUp<DefinitionType>(resolver);
                    this.CleanUp<DataType>(resolver);
                }
                if (this._settings.ImportMarketingFoundation)
                {
                    this.Import<Campaign>(resolver);
                    this.ImportTargets<CategoryTarget>(resolver);
                    this.ImportTargets<DynamicOrderPropertyTarget>(resolver);
                    this.ImportTargets<OrderAmountTarget>(resolver);
                    this.ImportTargets<ProductCatalogGroupTarget>(resolver);
                    this.ImportTargets<ProductCatalogTarget>(resolver);
                    this.ImportTargets<ProductTarget>(resolver);
                    this.ImportTargets<QuantityTarget>(resolver);
                    this.ImportTargets<VoucherTarget>(resolver);
                    if (this._versionImporter != null)
                        this._versionImporter.ImportMarketingFoundationTargets(resolver);
                    this.ImportAwards<AmountOffOrderLinesAward>(resolver);
                    this.ImportAwards<AmountOffOrderTotalAward>(resolver);
                    this.ImportAwards<AmountOffUnitAward>(resolver);
                    this.ImportAwards<DiscountSpecificOrderLineAward>(resolver);
                    this.ImportAwards<PercentOffOrderLinesAward>(resolver);
                    this.ImportAwards<PercentOffOrderTotalAward>(resolver);
                    this.ImportAwards<PercentOffShippingTotalAward>(resolver);
                    if (this._versionImporter != null)
                        this._versionImporter.ImportMarketingFoundationAwards(resolver);
                    if (this._settings.PerformCleanUp)
                        this.CleanUp<Campaign>(resolver);
                }
                if (CurrentUCommerceVersion() < SkipRolesImportVersion)
                    this.ImportRoles();
                AfterImportHandler afterImportHandler = OnAfterImport;
                if (afterImportHandler == null)
                    return;
                this.Log.Info("## Invoking OnAfterImport event handler ##");
                try
                {
                    afterImportHandler(this.SyncDataPath, this.Log);
                    this.Log.Info("OnBeforeImport event handler invoked");
                }
                catch (Exception ex)
                {
                    this.Log.Error(ex, "OnAfterImport event handler threw an exception", new object[0]);
                }
            }
        }

        private void ImportRoles()
        {
            this.Log.Info("## Importing user roles ##");
            IUserService iuserService = ObjectFactory.Instance.Resolve<IUserService>();
            IRoleService iroleService = ObjectFactory.Instance.Resolve<IRoleService>();
            List<User> list = Enumerable.ToList<User>(Enumerable.Where<User>((IEnumerable<User>)iuserService.GetAllUsers(), (Func<User, bool>)(u => u.IsAdmin)));
            IList<Role> allRoles = iroleService.GetAllRoles();
            using (List<User>.Enumerator enumerator = list.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    User current = enumerator.Current;
                    this.Log.Info("- adding admin user {0} to all roles ({1})", (object)current.UserName, (object)((ICollection<Role>)allRoles).Count);
                    iroleService.AddUserToRoles(current, allRoles);
                }
            }
        }

        private List<T> Import<T>(EntityReferenceResolver resolver) where T : class, new()
        {
            Type type = typeof(T);
            this.Log.Info("## Importing entities of type {0} ##", (object)type);
            List<T> list = new List<T>();
            try
            {
                foreach (XElement entityXml in this.LoadImportEntities(type))
                {
                    this.Log.Info("Parsing entity ({0})", (object)type);
                    object obj1 = this.ParseEntity(type, entityXml, (IEntityReferenceResolver)resolver);
                    string entityIdentifier = GetEntityIdentifier(obj1);
                    this.Log.Info("- entity has identifier \"{0}\"", (object)entityIdentifier, (object)type);
                    T obj2 = resolver.GetReferencedEntity(entityIdentifier, type) as T;
                    if ((object)obj2 == null)
                    {
                        this.Log.Info("- could not find local entity with identifier \"{0}\", creating a new one and re-parsing", (object)entityIdentifier);
                        obj2 = Activator.CreateInstance<T>();
                        resolver.RegisterCreatedEntity((object)obj2, entityIdentifier, type);
                        obj1 = this.ParseEntity(type, entityXml, (IEntityReferenceResolver)resolver);
                    }
                    this.Log.Info("Importing entity with identifier \"{0}\" ({1})", (object)entityIdentifier, (object)type);
                    this.SynchronizeEntities(obj1, (object)obj2, (IEntityReferenceResolver)resolver, (object)obj2);
                    list.Add(obj2);
                }
                this.Save<T>((IEnumerable<T>)list);
            }
            catch (Exception ex)
            {
                this.Log.Error(ex, "An exception occurred while importing entities of type {0}", (object)type);
                throw;
            }
            return list;
        }

        private List<Category> GetChildCategories(Category category)
        {
            List<Category> list = Enumerable.ToList<Category>((IEnumerable<Category>)category.Categories);
            using (IEnumerator<Category> enumerator = ((IEnumerable<Category>)category.Categories).GetEnumerator())
            {
                while (((IEnumerator)enumerator).MoveNext())
                {
                    Category current = enumerator.Current;
                    list.AddRange((IEnumerable<Category>)this.GetChildCategories(current));
                }
            }
            return list;
        }

        private List<T> CleanUp<T>(EntityReferenceResolver resolver, List<T> existingEntities = null, List<object> importEntities = null) where T : class, new()
        {
            Type type = typeof(T);
            this.Log.Info("## Cleaning up entities of type {0} ##", (object)type);
            try
            {
                Repository<T> repository = this.GetRepository<T>();
                if (existingEntities == null)
                {
                    existingEntities = resolver.GetAll<T>(type);
                    if (existingEntities == null)
                    {
                        this.Log.Error("Could not get all existing entities");
                        return (List<T>)null;
                    }
                }
                List<string> importEntityIdentifiers;
                if (importEntities == null)
                {
                    importEntityIdentifiers = new List<string>();
                    importEntities = new List<object>();
                    foreach (XElement entityXml in this.LoadImportEntities(type))
                    {
                        this.Log.Info("Parsing entity ({0})", (object)type);
                        object entity = this.ParseEntity(type, entityXml, (IEntityReferenceResolver)resolver);
                        importEntities.Add(entity);
                        string entityIdentifier = GetEntityIdentifier(entity);
                        this.Log.Info("- entity has identifier \"{0}\"", (object)entityIdentifier);
                        importEntityIdentifiers.Add(entityIdentifier);
                        if (typeof(T) == typeof(Category))
                        {
                            this.Log.Info("- identifying all children");
                            List<string> list =
                                Enumerable.ToList<string>(
                                    Enumerable.Select<Category, string>(
                                        (IEnumerable<Category>)this.GetChildCategories(entity as Category),
                                        new Func<Category, string>(GetEntityIdentifier)));
                            this.Log.Info("  - found {0} children{1}", (object)list.Count,
                                list.Count == 0
                                    ? (object)string.Empty
                                    : (object)string.Format(": {0}", (object)string.Join(", ", (IEnumerable<string>)list)));
                            importEntityIdentifiers.AddRange((IEnumerable<string>)list);
                        }
                    }
                }
                else
                {
                    importEntityIdentifiers = importEntities.Select(GetEntityIdentifier).ToList();
                }
                List<T> list1 = existingEntities.Where(e => !importEntityIdentifiers.Contains(GetEntityIdentifier(e))).ToList();
                if (!Enumerable.Any<T>((IEnumerable<T>)list1))
                    this.Log.Info("No entities to delete");
                if (typeof(T) == typeof(Product))
                {
                    List<Product> productsToDelete = Enumerable.ToList(Enumerable.OfType<Product>(list1));
                    foreach (var product in productsToDelete)
                    {
                        if (product.IsVariant)
                        {
                            if (productsToDelete.All(p => p.ParentProduct != product))
                            {
                                list1.Remove(product as T);
                            }
                        }
                    }
                }
                foreach (T obj in list1)
                {
                    this.Log.Info("Deleting entity \"{0}\"", (object)GetEntityIdentifier((object)obj));
                    if ((object)obj is ISoftDeletableEntity)
                    {
                        ((object)obj as ISoftDeletableEntity).Deleted = true;
                        repository.Save(obj);
                    }
                    else
                        repository.Delete(obj);
                }
                if (type == typeof(DefinitionType))
                {
                    List<Definition> list2 = Enumerable.ToList<Definition>(Enumerable.SelectMany<DefinitionType, Definition>(Enumerable.OfType<DefinitionType>((IEnumerable)Enumerable.Except<T>((IEnumerable<T>)existingEntities, (IEnumerable<T>)list1)), (Func<DefinitionType, IEnumerable<Definition>>)(d => (IEnumerable<Definition>)d.Definitions)));
                    List<string> importDefinitionIdentifiers = Enumerable.ToList<string>(Enumerable.SelectMany<DefinitionType, string>(Enumerable.OfType<DefinitionType>((IEnumerable)importEntities), (Func<DefinitionType, IEnumerable<string>>)(d => Enumerable.Select<Definition, string>((IEnumerable<Definition>)d.Definitions, new Func<Definition, string>(GetEntityIdentifier)))));
                    using (List<Definition>.Enumerator enumerator = Enumerable.ToList<Definition>(Enumerable.Where<Definition>((IEnumerable<Definition>)list2, (Func<Definition, bool>)(d => !importDefinitionIdentifiers.Contains(GetEntityIdentifier((object)d))))).GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Definition current = enumerator.Current;
                            current.Deleted = true;
                            current.Save();
                        }
                    }
                }
                if (type == typeof(Campaign))
                {
                    List<CampaignItem> list2 = Enumerable.ToList<CampaignItem>(Enumerable.SelectMany<Campaign, CampaignItem>(Enumerable.OfType<Campaign>((IEnumerable)Enumerable.Except<T>((IEnumerable<T>)existingEntities, (IEnumerable<T>)list1)), (Func<Campaign, IEnumerable<CampaignItem>>)(c => (IEnumerable<CampaignItem>)c.CampaignItems)));
                    List<string> importCampaignItemIdentifiers = Enumerable.ToList<string>(Enumerable.SelectMany<Campaign, string>(Enumerable.OfType<Campaign>((IEnumerable)importEntities), (Func<Campaign, IEnumerable<string>>)(c => Enumerable.Select<CampaignItem, string>((IEnumerable<CampaignItem>)c.CampaignItems, new Func<CampaignItem, string>(GetEntityIdentifier)))));
                    using (List<CampaignItem>.Enumerator enumerator = Enumerable.ToList<CampaignItem>(Enumerable.Where<CampaignItem>((IEnumerable<CampaignItem>)list2, (Func<CampaignItem, bool>)(c => !importCampaignItemIdentifiers.Contains(GetEntityIdentifier((object)c))))).GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            CampaignItem current = enumerator.Current;
                            current.Deleted = true;
                            current.Save();
                        }
                    }
                }
                return list1;
            }
            catch (Exception ex)
            {
                this.Log.Error(ex, "An exception occurred while importing entities of type {0}", (object)type);
                throw;
            }
        }

        public List<T> ImportTargets<T>(EntityReferenceResolver resolver) where T : Target, new()
        {
            return new TargetImporter(this).ImportEntities<T>(resolver);
        }

        internal List<T> ImportAwards<T>(EntityReferenceResolver resolver) where T : Award, new()
        {
            return new AwardImporter(this).ImportEntities<T>(resolver);
        }

        private static void SetPropertyValue(object entity, PropertyInfo property, object propertyValue)
        {
            object obj = property.GetValue(entity, (object[])null);
            if (obj != null && obj.Equals(propertyValue))
                return;
            property.SetValue(entity, propertyValue, (object[])null);
        }

        private void Save<T>(IEnumerable<T> entities) where T : class
        {
            this.GetRepository<T>().Save(entities);
        }

        private void Save(object entity)
        {
            MethodInfo method = entity.GetType().GetMethod("Save");
            if (!(method != (MethodInfo)null))
                throw new ArgumentException("Could not find Save() method on entity type " + (object)entity.GetType());
            method.Invoke(entity, (object[])null);
        }

        private static void InitializeCollection(object target, Type propertyType, PropertyInfo property)
        {
            Type type = typeof(List<>).MakeGenericType(GetCollectionGenericType(propertyType, property));
            property.SetValue(target, Activator.CreateInstance(type), (object[])null);
        }

        private static Type GetCollectionGenericType(Type propertyType, PropertyInfo property)
        {
            Type type = Enumerable.FirstOrDefault<Type>((IEnumerable<Type>)propertyType.GetGenericArguments());
            if (type == (Type)null)
                throw new ArgumentException("Expected a generic collection type for property " + property.Name);
            return type;
        }

        private object GetReferencedEntity(Type entityType, XElement itemXml, IEntityReferenceResolver resolver)
        {
            object obj = (object)null;
            if (itemXml.Name == (XName)"Entity")
                obj = this.ParseEntity(entityType, itemXml, resolver);
            else if (itemXml.Name == (XName)"EntityRef")
                obj = resolver.GetReferencedEntity(itemXml.Attribute((XName)"id").Value, entityType);
            return obj;
        }

        public MethodInfo GetAll(Type entityType)
        {
            MethodInfo method = entityType.GetMethod("All", BindingFlags.Static | BindingFlags.Public);
            if (!(method != (MethodInfo)null) || !IsCollection(method.ReturnType))
                return (MethodInfo)null;
            return method;
        }

        public Repository<T> GetRepository<T>() where T : class
        {
            return new Repository<T>((ISessionProvider)ObjectFactory.Instance.Resolve<ISessionProvider>());
        }

        public IEnumerable<XElement> LoadImportEntities(Type type)
        {
            return XDocument.Load(this.XmlPathFor(type)).Root.Elements((XName)"Entity");
        }

        public object ParseEntity(Type entityType, XElement entityXml, IEntityReferenceResolver resolver)
        {
            object instance = Activator.CreateInstance(entityType);
            PropertyInfo[] properties = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
            IEnumerable<XElement> source = entityXml.Elements((XName)"Property");
            foreach (PropertyInfo propertyInfo in properties)
            {
                PropertyInfo property = propertyInfo;
                Type type = property.PropertyType;
                if (!IsCollection(type) && property.GetSetMethod() == null && !property.CanWrite)
                {
                    this.Log.Info("- skipped parsing property {0} (no setter)", (object)property.Name);
                }
                else
                {
                    XElement xelement = Enumerable.FirstOrDefault<XElement>(source, (Func<XElement, bool>)(p => NameFromAttribute(p) == property.Name));
                    if (xelement == null)
                        this.Log.Info("- skipped parsing property {0} (no import data)", (object)property.Name);
                    else if (IsCollection(type))
                    {
                        this.Log.Info("- parsing collection property {0}...", (object)property.Name);
                        object obj = property.GetValue(instance, (object[])null);
                        if (obj == null)
                        {
                            this.Log.Info("  - creating missing collection on entity");
                            InitializeCollection(instance, type, property);
                            obj = property.GetValue(instance, (object[])null);
                        }
                        if (type.IsInterface)
                            type = obj.GetType();
                        Type collectionGenericType = GetCollectionGenericType(type, property);
                        foreach (XElement itemXml in xelement.Elements())
                        {
                            object referencedEntity = this.GetReferencedEntity(collectionGenericType, itemXml, resolver);
                            if (referencedEntity != null)
                                type.GetMethod("Add").Invoke(obj, new object[1]
                {
                  referencedEntity
                });
                        }
                        this.Log.Info("- collection property {0} completed", (object)property.Name);
                    }
                    else if (IsComplexType(type))
                    {
                        this.Log.Info("- parsing complex property {0}", (object)property.Name);
                        XElement itemXml = Enumerable.FirstOrDefault<XElement>(xelement.Elements(), (Func<XElement, bool>)(e =>
                        {
                            if (!(e.Name == (XName)"Entity"))
                                return e.Name == (XName)"EntityRef";
                            return true;
                        }));
                        object obj = itemXml != null ? this.GetReferencedEntity(type, itemXml, resolver) : (object)null;
                        property.SetValue(instance, obj, (object[])null);
                    }
                    else
                    {
                        this.Log.Info("- parsing simple property {0}", (object)property.Name);
                        XElement valueXml = xelement.Element((XName)"Value");
                        if (this.IsNull(valueXml))
                        {
                            SetPropertyValue(instance, property, (object)null);
                        }
                        else
                        {
                            object propertyValue = TypeDescriptor.GetConverter(type).ConvertFromInvariantString(valueXml.Value);
                            SetPropertyValue(instance, property, propertyValue);
                        }
                    }
                }
            }
            return instance;
        }

        public string GetEntitySignature(object entity)
        {
            Type type = entity.GetType();
            List<PropertyInfo> list1 = Enumerable.ToList<PropertyInfo>(Enumerable.Where<PropertyInfo>((IEnumerable<PropertyInfo>)type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty), (Func<PropertyInfo, bool>)(p =>
            {
                if (!IsCollection(p.PropertyType) && this.CanSyncProperty(entity, p))
                    return p.GetSetMethod() != (MethodInfo)null;
                return false;
            })));
            List<object> list2 = new List<object>();
            foreach (PropertyInfo propertyInfo in (IEnumerable<PropertyInfo>)Enumerable.OrderBy<PropertyInfo, string>((IEnumerable<PropertyInfo>)list1, (Func<PropertyInfo, string>)(p => p.Name)))
            {
                object entity1 = propertyInfo.GetValue(entity, (object[])null);
                if (entity1 != null)
                {
                    if (IsComplexType(propertyInfo.PropertyType))
                    {
                        if (IsLocalEntityFor(entity, propertyInfo.PropertyType))
                        {
                            string entityIdentifier = GetEntityIdentifier(entity1);
                            if (entityIdentifier != null)
                                list2.Add((object)string.Format(">{0}", (object)entityIdentifier));
                        }
                    }
                    else
                        list2.Add(entity1);
                }
            }
            return string.Format("{0}|{1}", (object)type.Name, (object)string.Join<object>("|", (IEnumerable<object>)list2));
        }

        public void SynchronizeEntities(object source, object target, IEntityReferenceResolver resolver, object parent)
        {
            if (target == null)
                throw new ArgumentException("No target object supplied");
            if (source.GetType() != ActualTypeFrom((object)target.GetType()) && !source.GetType().IsInstanceOfType(target))
                throw new ArgumentException("Source and target types must be the same");
            Type type1 = parent.GetType();
            Type type2 = source.GetType();
            foreach (PropertyInfo propertyInfo in type2.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty))
            {
                if (!this.CanSyncProperty(target, propertyInfo))
                {
                    this.Log.Info("- skipped importing property {0} (not sync-able)", (object)propertyInfo.Name);
                }
                else
                {
                    Type type3 = propertyInfo.PropertyType;
                    if (!IsCollection(type3) && propertyInfo.GetSetMethod() == (MethodInfo)null)
                    {
                        this.Log.Info("- skipped importing property {0} (no setter)", (object)propertyInfo.Name);
                    }
                    else
                    {
                        object obj1 = propertyInfo.GetValue(source, (object[])null);
                        object target1 = propertyInfo.GetValue(target, (object[])null);
                        if (IsCollection(type3))
                        {
                            this.Log.Info("- importing collection property {0}...", (object)propertyInfo.Name);
                            if (target1 == null)
                            {
                                this.Log.Info("  - creating missing collection on entity");
                                InitializeCollection(target, type3, propertyInfo);
                                target1 = propertyInfo.GetValue(target, (object[])null);
                            }
                            if (type3.IsInterface)
                                type3 = target1.GetType();
                            MethodInfo method1 = type3.GetMethod("Add");
                            MethodInfo method2 = type3.GetMethod("Remove");
                            List<object> sourceItems = Enumerable.ToList<object>(Enumerable.OfType<object>(obj1 as IEnumerable));
                            List<object> targetItems = Enumerable.ToList<object>(Enumerable.OfType<object>(target1 as IEnumerable));
                            List<object> list1 = Enumerable.ToList<object>(Enumerable.Where<object>((IEnumerable<object>)targetItems, (Func<object, bool>)(t => !Enumerable.Any<object>((IEnumerable<object>)sourceItems, (Func<object, bool>)(s => GetEntityIdentifier(s) == GetEntityIdentifier(t))))));
                            if (Enumerable.Any<object>((IEnumerable<object>)list1))
                                this.Log.Info("- removing {0} obsolete item(s)", (object)list1.Count);
                            foreach (object entity in list1)
                            {
                                if (IsLocalEntityFor(entity, type2, propertyInfo))
                                {
                                    if (entity is ISoftDeletableEntity)
                                    {
                                        (entity as ISoftDeletableEntity).Deleted = true;
                                    }
                                    else
                                    {
                                        method2.Invoke(target1, new object[1]
                    {
                      entity
                    });
                                        MethodInfo method3 = entity.GetType().GetMethod("Delete", BindingFlags.Instance | BindingFlags.Public);
                                        if (method3 != (MethodInfo)null)
                                            method3.Invoke(entity, (object[])null);
                                        else
                                            this.Log.Info("  - unable to delete obsolete item: {0}", entity is INamed ? (object)(entity as INamed).Name : (entity is IEntity ? (object)(entity as IEntity).Id.ToString() : (object)"(N/A)"));
                                    }
                                }
                                else
                                    method2.Invoke(target1, new object[1]
                  {
                    entity
                  });
                                targetItems.Remove(entity);
                            }
                            if (Enumerable.Any<object>((IEnumerable<object>)targetItems))
                                this.Log.Info("- updating {0} item(s)", (object)targetItems.Count);
                            foreach (object obj2 in targetItems)
                            {
                                object targetItem = obj2;
                                object source1 = Enumerable.FirstOrDefault<object>((IEnumerable<object>)sourceItems, (Func<object, bool>)(s => GetEntityIdentifier(s) == GetEntityIdentifier(targetItem)));
                                if (source1 is DefinitionField)
                                {
                                    var definitionField = source1 as DefinitionField;
                                    if (definitionField.DataType == null)
                                    {
                                        continue;
                                    }
                                }
                                if (source1 != null && IsLocalEntityFor(targetItem, type2, propertyInfo))
                                    this.SynchronizeEntities(source1, targetItem, resolver, target);
                            }
                            List<object> list2 = Enumerable.ToList<object>(Enumerable.Where<object>((IEnumerable<object>)sourceItems, (Func<object, bool>)(s => !Enumerable.Any<object>((IEnumerable<object>)targetItems, (Func<object, bool>)(t => GetEntityIdentifier(t) == GetEntityIdentifier(s))))));
                            if (Enumerable.Any<object>((IEnumerable<object>)list2))
                                this.Log.Info("- adding {0} item(s)", (object)list2.Count);
                            foreach (object obj2 in list2)
                            {
                                object target2;
                                if (IsLocalEntityFor(obj2, type2, propertyInfo))
                                {
                                    target2 = Activator.CreateInstance(obj2.GetType());
                                    this.SynchronizeEntities(obj2, target2, resolver, target);
                                }
                                else
                                    target2 = resolver.GetReferencedEntity(GetEntityIdentifier(obj2), obj2.GetType());
                                if (target2 != null)
                                    method1.Invoke(target1, new object[1]
                  {
                    target2
                  });
                            }
                            this.Log.Info("- collection property {0} completed", (object)propertyInfo.Name);
                        }
                        else if (IsComplexType(type3))
                        {
                            this.Log.Info("- importing complex property {0}", (object)propertyInfo.Name);
                            if (obj1 == null)
                            {
                                if (type3 == type1 && propertyInfo.Name == type1.Name)
                                {
                                    this.Log.Info("  - importing property value as parent");
                                    obj1 = parent;
                                }
                                propertyInfo.SetValue(target, obj1, (object[])null);
                            }
                            else if (!IsLocalEntityFor(obj1, type2, propertyInfo))
                            {
                                object referencedEntity = resolver.GetReferencedEntity(GetEntityIdentifier(obj1), type3);
                                propertyInfo.SetValue(target, referencedEntity, (object[])null);
                            }
                            else
                                this.SynchronizeEntities(obj1, target1, resolver, target);
                        }
                        else
                        {
                            this.Log.Info("- importing simple property {0}", (object)propertyInfo.Name);
                            if (type3 == typeof(string) && this._settings.CmsProvider != null)
                            {
                                if (IsMediaIdProperty(propertyInfo))
                                {
                                    obj1 = (object)this._settings.CmsProvider.IdentifierToMediaId(obj1 as string);
                                    this.Log.Info("  - CMS provider translated property value to media identifier: {0}", obj1 ?? (object)"(null)");
                                }
                                else if (IsContentIdProperty(propertyInfo))
                                {
                                    obj1 = (object)this._settings.CmsProvider.IdentifierToContentId(obj1 as string);
                                    this.Log.Info("  - CMS provider translated property value to content identifier: {0}", obj1 ?? (object)"(null)");
                                }
                            }
                            SetPropertyValue(target, propertyInfo, obj1);
                        }
                    }
                }
            }
            if (!this.ShouldSaveTargetEntityDuringImport(target, parent))
                return;
            this.Log.Info("- explicitly saving entity");
            this.Save(target);
        }

        protected bool CanSyncProperty(object entity, PropertyInfo property)
        {
            if (entity is OrderNumberSerie && property.Name == "CurrentNumber" || entity is ProductCatalogGroup && property.Name == "ProductCatalogs")
                return false;
            if (entity is ProductDefinitionFieldDescription)
            {
                if (property.Name == "DisplayName" || property.Name == "Description")
                    return true;
                ProductDefinitionFieldDescription fieldDescription = entity as ProductDefinitionFieldDescription;
                if (fieldDescription.ProductDefinitionField != null || !(property.Name == "ProductDefinitionField"))
                    return string.IsNullOrEmpty(fieldDescription.CultureCode);
                return true;
            }
            return CanExportProperty(entity, property);
        }

        private bool ShouldSaveTargetEntityDuringImport(object target, object parent)
        {
            return target.GetType() == typeof(Definition);
        }

        public List<Product> ImportProducts()
        {
            this.Log.Info("## Importing products ##");
            List<Product> list1 = new List<Product>();
            List<XElement> list2 = Enumerable.ToList<XElement>(this.LoadImportEntities(typeof(Product)));
            Dictionary<Product, XElement> dictionary = new Dictionary<Product, XElement>();
            foreach (XElement node in list2)
            {
                try
                {
                    Product key = this.ParseProduct(node);
                    dictionary.Add(key, node);
                    list1.Add(key);
                }
                catch (Exception ex)
                {
                    this.Log.Error(ex, "Could not parse product", new object[0]);
                    throw;
                }
            }
            foreach (var product in list1.Where(x => string.IsNullOrEmpty(x.VariantSku)))
            {
                var currentProduct = product;
                foreach (var variant in list1.Where(x => !string.IsNullOrEmpty(x.VariantSku) && x.Sku == currentProduct.Sku))
                {
                    currentProduct.AddVariant(variant);
                }
                currentProduct.Save();
            }

            using (Dictionary<Product, XElement>.Enumerator enumerator1 = dictionary.GetEnumerator())
            {
                while (enumerator1.MoveNext())
                {
                    KeyValuePair<Product, XElement> current1 = enumerator1.Current;
                    Product key = current1.Key;
                    IEnumerable<XElement> propertyEntities = this.GetPropertyEntities(current1.Value, "ProductRelations");
                    var relatedProductIdentifier = propertyEntities.Select(e => GetEntityRefIdentifier(e, "RelatedProduct")).ToList();
                    // ISSUE: reference to a compiler-generated method
                    using (List<ProductRelation>.Enumerator enumerator2 = key.ProductRelations.Where(x => relatedProductIdentifier.Contains(x.Id.ToString())).ToList().GetEnumerator())
                    {
                        while (enumerator2.MoveNext())
                        {
                            ProductRelation current2 = enumerator2.Current;
                            key.ProductRelations.Remove(current2);
                            current2.Delete();
                        }
                    }
                    foreach (XElement entityXml in propertyEntities)
                    {
                        var productRelationTypeIdentifier = this.GetEntityRefIdentifier(entityXml, "ProductRelationType");
                        ProductRelationType productRelationType = ProductRelationType.FirstOrDefault(x => x.Name == productRelationTypeIdentifier);
                        if (productRelationType == null)
                        {
                            // ISSUE: reference to a compiler-generated field
                            throw new ArgumentException(string.Format("Could not find product relation type: {0}", (object)productRelationTypeIdentifier));
                        }
                        // ISSUE: reference to a compiler-generated field
                        var relatedProductIdentifierFromPropertyEntity = this.GetEntityRefIdentifier(entityXml, "RelatedProduct");
                        // ISSUE: reference to a compiler-generated method
                        Product product = list1.FirstOrDefault(x => relatedProductIdentifierFromPropertyEntity.Contains(x.Name));
                        if (product == null)
                        {
                            // ISSUE: reference to a compiler-generated field
                            throw new ArgumentException(string.Format("Could not find related product: {0}", relatedProductIdentifierFromPropertyEntity));
                        }
                        // ISSUE: reference to a compiler-generated method
                        ProductRelation productRelation1 = key.ProductRelations.FirstOrDefault(x => x.Id.ToString() == relatedProductIdentifierFromPropertyEntity);
                        if (productRelation1 == null)
                        {
                            ProductRelation productRelation2 = new ProductRelation();
                            productRelation2.Product = (key);
                            productRelation2.RelatedProduct = (product);
                            productRelation1 = productRelation2;
                            key.ProductRelations.Add(productRelation1);
                        }
                        productRelation1.ProductRelationType = (productRelationType);
                    }
                }
            }
            return list1;
        }

        private Product ParseProduct(XElement node)
        {
            var sku = this.GetPropertyValue(node, "Sku");
            var variantSku = this.GetPropertyValue(node, "VariantSku");
            string str = string.Format("SKU: {0}{1}", (object)sku, string.IsNullOrEmpty(variantSku) ? (object)string.Empty : (object)string.Format(", variant SKU: {0}", (object)variantSku));
            Log.Info("- importing product: {0}", (object)str);
            var productDefinitionRefIdentifier = this.GetEntityRefIdentifier(node, "ProductDefinition");
            var productDefinition = ProductDefinition.FirstOrDefault(x => x.Name == productDefinitionRefIdentifier);

            if (productDefinition == null)
            {
                throw new ArgumentException(string.Format("Could not find product definition: {0} for product: {1}", (object)productDefinitionRefIdentifier, (object)str));
            }

            Product product1 = Product.FirstOrDefault(x => x.Sku == sku && x.VariantSku == variantSku);
            if (product1 == null)
            {
                Product product2 = new Product();
                product2.Sku = sku;
                product2.VariantSku = variantSku;
                product1 = product2;
            }
            product1.ParentProduct = null;
            product1.ProductDefinition = (productDefinition);
            product1.Name = (this.GetPropertyValue(node, "Name"));
            product1.DisplayOnSite = (this.GetPropertyValueAsBoolean(node, "DisplayOnSite"));
            if (this._settings.CmsProvider != null)
            {
                product1.ThumbnailImageMediaId = (this._settings.CmsProvider.IdentifierToMediaId(this.GetPropertyValue(node, "ThumbnailImageMediaId")));
                product1.PrimaryImageMediaId = (this._settings.CmsProvider.IdentifierToMediaId(this.GetPropertyValue(node, "PrimaryImageMediaId")));
            }
            else
            {
                product1.ThumbnailImageMediaId = (this.GetPropertyValue(node, "ThumbnailImageMediaId"));
                product1.PrimaryImageMediaId = (this.GetPropertyValue(node, "PrimaryImageMediaId"));
            }
            product1.Weight = (this.GetPropertyValueAsDecimal(node, "Weight"));
            product1.AllowOrdering = (this.GetPropertyValueAsBoolean(node, "AllowOrdering"));
            product1.ModifiedBy = (this.GetPropertyValue(node, "ModifiedBy"));
            product1.ModifiedOn = (this.GetPropertyValueAsDateTime(node, "ModifiedOn"));
            product1.CreatedOn = (this.GetPropertyValueAsDateTime(node, "CreatedOn"));
            product1.CreatedBy = (this.GetPropertyValue(node, "CreatedBy"));
            string propertyValue = this.GetPropertyValue(node, "Rating");
            product1.Rating = (string.IsNullOrEmpty(propertyValue) ? new double?() : new double?(Convert.ToDouble(propertyValue, (IFormatProvider)CultureInfo.InvariantCulture)));
            IEnumerable<XElement> categoryProductRelationsXml = this.GetPropertyEntities(node, "CategoryProductRelations");

            // ISSUE: reference to a compiler-generated field
            var categoryEntityIdentifiers = Enumerable.ToList<string>(Enumerable.Select<XElement, string>(categoryProductRelationsXml, (Func<XElement, string>)(e => this.GetEntityRefIdentifier(e, "Category"))));
            // ISSUE: reference to a compiler-generated method
            using (List<CategoryProductRelation>.Enumerator enumerator = product1.CategoryProductRelations.Where(x => !categoryEntityIdentifiers.Contains(x.Name)).ToList().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    CategoryProductRelation current = enumerator.Current;
                    product1.CategoryProductRelations.Remove(current);
                    current.Delete();
                }
            }
            foreach (XElement entityXml in categoryProductRelationsXml)
            {
                var categoryIdentifier = this.GetEntityRefIdentifier(entityXml, "Category");
                CategoryProductRelation categoryProductRelation1 = product1.CategoryProductRelations.FirstOrDefault(x => x.Name == categoryIdentifier);
                if (categoryProductRelation1 == null)
                {
                    Category category = Category.FirstOrDefault(x => x.Name == categoryIdentifier);
                    if (category == null)
                    {
                        // ISSUE: reference to a compiler-generated field
                        throw new ArgumentException(string.Format("Could not find category: {0} for product: {1}", categoryIdentifier, str));
                    }
                    CategoryProductRelation categoryProductRelation2 = new CategoryProductRelation();
                    categoryProductRelation2.Category = (category);
                    categoryProductRelation2.Product = (product1);
                    categoryProductRelation1 = categoryProductRelation2;
                    product1.CategoryProductRelations.Add(categoryProductRelation1);
                }
                categoryProductRelation1.SortOrder = (this.GetPropertyValueAsInteger(entityXml, "SortOrder"));
            }
            IEnumerable<XElement> priceGroupPricesXml = this.GetPropertyEntities(node, "PriceGroupPrices");
            // ISSUE: reference to a compiler-generated field
            var priceGroupPricesIdentifier = Enumerable.ToList<string>(Enumerable.Select<XElement, string>(priceGroupPricesXml, (Func<XElement, string>)(e => this.GetEntityRefIdentifier(e, "PriceGroup"))));
            // ISSUE: reference to a compiler-generated method
            using (List<PriceGroupPrice>.Enumerator enumerator = product1.PriceGroupPrices.Where(x => priceGroupPricesIdentifier.Contains(x.PriceGroup.Name)).ToList().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    PriceGroupPrice current = enumerator.Current;
                    product1.PriceGroupPrices.Remove(current);
                    current.Delete();
                }
            }
            foreach (XElement entityXml in priceGroupPricesXml)
            {
                var priceGroupIdentifier = this.GetEntityRefIdentifier(entityXml, "PriceGroup");
	            if (string.IsNullOrWhiteSpace(priceGroupIdentifier))
	            {
		            throw new ArgumentException("Price group identifier was null");
	            }

	            var priceGroupIDentifierToLower = priceGroupIdentifier.ToLower();

                // ISSUE: reference to a compiler-generated method
                PriceGroupPrice priceGroupPrice1 = product1.PriceGroupPrices.FirstOrDefault(x => x.PriceGroup.Name.ToLower() == priceGroupIDentifierToLower);
                if (priceGroupPrice1 == null)
                {
                    PriceGroup priceGroup = PriceGroup.FirstOrDefault(x => x.Name.ToLower() == priceGroupIDentifierToLower );

					if (priceGroup == null)
                    {
                        throw new ArgumentException(string.Format("Could not find price group: {0} for product: {1}", (object)priceGroupIdentifier, (object)str));
                    }

	                //priceGroupPrice1 = PriceGroupPrice.FirstOrDefault(o => o.PriceGroupPriceId == priceGroup.Id);

                    //if (priceGroupPrice1 == null)
                    //{
                    //    // ISSUE: reference to a compiler-generated field
                    //    throw new ArgumentException(string.Format("Could not find price group: {0} for product: {1}", (object)//priceGroupIdentifier, (object)str));
                    //}

                    PriceGroupPrice priceGroupPrice2 = new PriceGroupPrice();
                    priceGroupPrice2.PriceGroup = (priceGroup);
                    priceGroupPrice2.Product = (product1);
                    priceGroupPrice1 = priceGroupPrice2;
                    product1.PriceGroupPrices.Add(priceGroupPrice1);
                }
                priceGroupPrice1.Price = (this.GetPropertyValueAsNullableDecimal(entityXml, "Price"));
                priceGroupPrice1.DiscountPrice = (this.GetPropertyValueAsNullableDecimal(entityXml, "DiscountPrice"));
            }
            IEnumerable<XElement> propertyEntities3 = this.GetPropertyEntities(node, "ProductDescriptions");
            // ISSUE: reference to a compiler-generated field
            var productDescriptionsCultureCodes = Enumerable.ToList<string>(Enumerable.Select<XElement, string>(propertyEntities3, (Func<XElement, string>)(e => this.GetPropertyValue(e, "CultureCode"))));
            // ISSUE: reference to a compiler-generated method
            using (List<ProductDescription>.Enumerator enumerator = product1.ProductDescriptions.Where(x => !productDescriptionsCultureCodes.Contains(x.CultureCode)).ToList().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    ProductDescription current = enumerator.Current;
                    product1.ProductDescriptions.Remove(current);
                    current.Delete();
                }
            }
            foreach (XElement entityXml1 in propertyEntities3)
            {
                var cultureCode = this.GetPropertyValue(entityXml1, "CultureCode");
                // ISSUE: reference to a compiler-generated method
                ProductDescription productDescription1 = product1.ProductDescriptions.FirstOrDefault(x => x.CultureCode == cultureCode);
                if (productDescription1 == null)
                {
                    ProductDescription productDescription2 = new ProductDescription();
                    productDescription2.Product = (product1);
                    // ISSUE: reference to a compiler-generated field
                    productDescription2.CultureCode = cultureCode;
                    productDescription1 = productDescription2;
                    product1.ProductDescriptions.Add(productDescription1);
                }
                productDescription1.DisplayName = GetPropertyValue(entityXml1, "DisplayName");
                productDescription1.ShortDescription = GetPropertyValue(entityXml1, "ShortDescription");
                productDescription1.LongDescription = GetPropertyValue(entityXml1, "LongDescription");
                IEnumerable<XElement> propertyEntities4 = this.GetPropertyEntities(entityXml1, "ProductDescriptionProperties");
                // ISSUE: reference to a compiler-generated field
                var productDefinitionFieldsFromProductDescriptionProperties = propertyEntities4.Select(e => GetEntityRefIdentifier(e, "ProductDefinitionField")).ToList();
                // ISSUE: reference to a compiler-generated method
                using (IEnumerator<ProductDescriptionProperty> enumerator = productDescription1.ProductDescriptionProperties.Where(x => !productDefinitionFieldsFromProductDescriptionProperties.Contains(x.ProductDefinitionField.Name)).GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        ProductDescriptionProperty current = enumerator.Current;
                        productDescription1.ProductDescriptionProperties.Remove(current);
                        current.Delete();
                    }
                }
                foreach (XElement entityXml2 in propertyEntities4)
                {
                    var productDefinitionFieldIdentifier = this.GetEntityRefIdentifier(entityXml2, "ProductDefinitionField");
                    ProductDescriptionProperty descriptionProperty1 = productDescription1.ProductDescriptionProperties.FirstOrDefault(x => x.ProductDefinitionField.Name == productDefinitionFieldIdentifier);
                    if (descriptionProperty1 == null)
                    {
                        // ISSUE: reference to a compiler-generated method
                        ProductDefinitionField productDefinitionField = ProductDefinitionField.All().ToList().FirstOrDefault(x => x.ProductDefinition.Name == productDefinitionFieldIdentifier);
                        if (productDefinitionField == null)
                        {
                            // ISSUE: reference to a compiler-generated field
                            throw new ArgumentException(string.Format("Could not find product defition field: {0} for product: {1}", (object)productDefinitionFieldIdentifier, (object)str));
                        }
                        ProductDescriptionProperty descriptionProperty2 = new ProductDescriptionProperty();
                        descriptionProperty2.ProductDescription = productDescription1;
                        descriptionProperty2.ProductDefinitionField = productDefinitionField;
                        descriptionProperty1 = descriptionProperty2;
                        productDescription1.ProductDescriptionProperties.Add(descriptionProperty1);
                    }
                    descriptionProperty1.Value = GetPropertyValue(entityXml2, "Value");
                    descriptionProperty1.CultureCode = GetPropertyValue(entityXml2, "CultureCode");
                }
            }
            IEnumerable<XElement> propertyEntities5 = this.GetPropertyEntities(node, "ProductProperties");
            // ISSUE: reference to a compiler-generated field
            var productDefinitionFieldFromProductPropertyIdentifier = Enumerable.ToList<string>(Enumerable.Select<XElement, string>(propertyEntities5, (Func<XElement, string>)(e => this.GetEntityRefIdentifier(e, "ProductDefinitionField"))));
            // ISSUE: reference to a compiler-generated method
            using (List<ProductProperty>.Enumerator enumerator = product1.ProductProperties.Where(x => productDefinitionFieldFromProductPropertyIdentifier.Contains(x.ProductDefinitionField.Name)).ToList().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    ProductProperty current = enumerator.Current;
                    product1.ProductProperties.Remove(current);
                    current.Delete();
                }
            }
            foreach (XElement entityXml in propertyEntities5)
            {
                var productDefinitionFieldIdentifier = this.GetEntityRefIdentifier(entityXml, "ProductDefinitionField");
                // ISSUE: reference to a compiler-generated method
                ProductProperty productProperty1 = product1.ProductProperties.FirstOrDefault(x => x.ProductDefinitionField.Name == productDefinitionFieldIdentifier);
                if (productProperty1 == null)
                {
                    // ISSUE: reference to a compiler-generated method
                    ProductDefinitionField productDefinitionField = ProductDefinitionField.All().ToList().FirstOrDefault(x => x.ProductDefinition.Name == productDefinitionFieldIdentifier);
                    if (productDefinitionField == null)
                    {
                        // ISSUE: reference to a compiler-generated field
                        throw new ArgumentException(string.Format("Could not find product definition field: {0} for product: {1}", (object)productDefinitionFieldIdentifier, (object)str));
                    }
                    ProductProperty productProperty2 = new ProductProperty();
                    productProperty2.Product = product1;
                    productProperty2.ProductDefinitionField = productDefinitionField;
                    productProperty1 = productProperty2;
                    product1.ProductProperties.Add(productProperty1);
                }
                productProperty1.Value = GetPropertyValue(entityXml, "Value");
            }
            return product1;
        }

        private string GetEntityRefIdentifier(XElement entityXml, string propertyName)
        {
            return GetPropertyXml(entityXml, propertyName).Element((XName)"EntityRef").Attribute((XName)"id").Value;
        }

        private DateTime GetPropertyValueAsDateTime(XElement entityXml, string propertyName)
        {
            return Convert.ToDateTime(this.GetPropertyValue(entityXml, propertyName) ?? "2000-01-01T00:00:00");
        }

        private Decimal? GetPropertyValueAsNullableDecimal(XElement entityXml, string propertyName)
        {
            string propertyValue = this.GetPropertyValue(entityXml, propertyName);
            if (string.IsNullOrEmpty(propertyValue))
                return new Decimal?();
            return new Decimal?(Convert.ToDecimal(propertyValue, (IFormatProvider)CultureInfo.InvariantCulture));
        }

        private Decimal GetPropertyValueAsDecimal(XElement entityXml, string propertyName)
        {
            return Convert.ToDecimal(this.GetPropertyValue(entityXml, propertyName) ?? "0.0000", (IFormatProvider)CultureInfo.InvariantCulture);
        }

        private bool GetPropertyValueAsBoolean(XElement entityXml, string propertyName)
        {
            return Convert.ToBoolean(this.GetPropertyValue(entityXml, propertyName) ?? "false");
        }

        private int GetPropertyValueAsInteger(XElement entityXml, string propertyName)
        {
            return Convert.ToInt32(this.GetPropertyValue(entityXml, propertyName) ?? "0");
        }

        private string GetPropertyValue(XElement entityXml, string propertyName)
        {
            XElement valueXml = GetPropertyXml(entityXml, propertyName).Element((XName)"Value");
            if (!this.IsNull(valueXml))
                return valueXml.Value;
            return (string)null;
        }

        private static XElement GetPropertyXml(XElement entityXml, string propertyName)
        {
            XElement xelement = Enumerable.FirstOrDefault<XElement>(entityXml.Elements((XName)"Property"), (Func<XElement, bool>)(e => e.Attribute((XName)"name").Value == propertyName));
            if (xelement == null)
                throw new ArgumentException("Could not find property: " + propertyName);
            return xelement;
        }

        private IEnumerable<XElement> GetPropertyEntities(XElement entityXml, string propertyName)
        {
            return GetPropertyXml(entityXml, propertyName).Elements((XName)"Entity");
        }

        private bool IsNull(XElement valueXml)
        {
            return valueXml.Attribute((XName)"isNull").Value == "1";
        }

        public delegate bool BeforeImportHandler(string syncDataPath, ILogger log);

        public delegate void AfterImportHandler(string syncDataPath, ILogger log);
    }

}