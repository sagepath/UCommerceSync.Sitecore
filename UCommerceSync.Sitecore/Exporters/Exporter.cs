using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;
using UCommerce.EntitiesV2;
using UCommerce.EntitiesV2.Definitions;
using UCommerceSync.Logging;
using UCommerceSync.Sitecore.VersionSpecific;
using UCommerceSync.VersionSpecific;

namespace UCommerceSync.Sitecore.Exporters
{
    public class Exporter : ImportExportBase, IExporter
    {
        public delegate void AfterExportHandler(string syncDataPath, ILogger log);

        public delegate bool BeforeExportHandler(string syncDataPath, ILogger log);

        public static BeforeExportHandler OnBeforeExport;
        public static AfterExportHandler OnAfterExport;
        private readonly ExportSettings _settings;
        private readonly IVersionSpecificExporter _versionExporter;
        private DateTime _exportDate = DateTime.MinValue;

        public Exporter(ExportSettings settings, string syncDataPath, ILogger log)
            : base(syncDataPath, log)
        {
            _settings = settings;
            if (!(CurrentUCommerceVersion() >= PriceGroupTargetVersion))
                return;
            _versionExporter = new VersionSpecificExporter_6_6_5_15100(this);
        }

        public void Export()
        {
            _exportDate = DateTime.UtcNow;
            var beforeExportHandler = OnBeforeExport;
            if (beforeExportHandler != null)
            {
                Log.Info("## Invoking OnBeforeExport event handler ##");
                try
                {
                    if (!beforeExportHandler(SyncDataPath, Log))
                    {
                        Log.Error("OnBeforeExport event handler returned false - aborting export");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "OnBeforeExport event handler threw an exception - aborting export");
                    return;
                }
                Log.Info("OnBeforeExport event handler invoked");
            }

            ExportEntitites(Country.All().ToList());
            ExportEntitites(Currency.All().ToList());
            ExportEntitites(OrderNumberSerie.All().ToList());
            ExportEntitites(ProductRelationType.All());
            ExportEntitites(PaymentMethod.All().ToList());
            ExportEntitites(ShippingMethod.All().ToList());
            ExportEntitites(EmailType.All().ToList());
            ExportEntitites(EmailProfile.All().ToList());
            ExportEntitites(ProductDefinition.All().ToList());
            ExportEntitites(DefinitionType.All().ToList());
            ExportEntitites(DataType.All().ToList());
            ExportEntitites(ExistingProductCatalogGroups());
            ExportEntitites(ExistingProductCatalogs());
            ExportEntitites(ExistingCategories());
            if (_settings.ExportProducts)
            {
                ExportEntitites(ExistingProducts());
            }
            ExportEntitites(PriceGroup.All().ToList());
            ExportEntitites(PriceGroupPrice.All().ToList());
            ExportEntitites(Campaign.All().ToList());
            ExportEntitites(ExportableTargets(CategoryTarget.All().ToList()));
            ExportEntitites(ExportableTargets(DynamicOrderPropertyTarget.All().ToList()));
            ExportEntitites(ExportableTargets(OrderAmountTarget.All().ToList()));
            ExportEntitites(ExportableTargets(ProductCatalogGroupTarget.All().ToList()));
            ExportEntitites(ExportableTargets(ProductCatalogTarget.All().ToList()));
            ExportEntitites(ExportableTargets(ProductTarget.All().ToList()));
            ExportEntitites(ExportableTargets(QuantityTarget.All().ToList()));
            ExportEntitites(ExportableTargets(VoucherTarget.All().ToList()));
            if (_versionExporter != null)
            {
                _versionExporter.ExportMarketingFoundationTargets();
            }
            ExportEntitites(ExportableAwards(AmountOffOrderLinesAward.All().ToList()));
            ExportEntitites(ExportableAwards(AmountOffOrderTotalAward.All().ToList()));
            ExportEntitites(ExportableAwards(AmountOffUnitAward.All().ToList()));
            ExportEntitites(ExportableAwards(DiscountSpecificOrderLineAward.All().ToList()));
            ExportEntitites(ExportableAwards(PercentOffOrderLinesAward.All().ToList()));
            ExportEntitites(ExportableAwards(PercentOffOrderTotalAward.All().ToList()));
            ExportEntitites(ExportableAwards(PercentOffShippingTotalAward.All().ToList()));
            if (_versionExporter != null)
                _versionExporter.ExportMarketingFoundationAwards();
            var afterExportHandler = OnAfterExport;
            if (afterExportHandler == null)
                return;
            Log.Info("## Invoking OnAfterExport event handler ##");
            try
            {
                afterExportHandler(SyncDataPath, Log);
                Log.Info("OnBeforeExport event handler invoked");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OnAfterExport event handler threw an exception");
            }
        }

        public void ExportEntitites<T>(IEnumerable<T> entities) where T : IEntity
        {
            var type = typeof (T);
            Log.Info("## Exporting entities of type {0} ##", (object) type);
            try
            {
                var xelement1 = new XElement(type.Name,
                    new XAttribute("version", CurrentUCommerceVersion()),
                    new XAttribute("timestamp", _exportDate));
                foreach (var obj in entities)
                {
                    Log.Info("Exporting entity with ID {0}{1} ({2})", (object) obj.Id,
                        (object) obj is INamed
                            ? (object) string.Format(" ({0})", ((object) obj as INamed).Name)
                            : (object) string.Empty, (object) type);
                    var xelement2 = ExportType(obj);
                    xelement1.Add(xelement2);
                }
                xelement1.Save(XmlPathFor(type));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception occurred while exporting entities of type {0}", (object) type);
                throw;
            }
        }

        public IEnumerable<T> ExportableTargets<T>(IEnumerable<T> targets) where T : Target
        {
            return targets.Where(t =>
            {
                if (!t.CampaignItem.Deleted)
                    return !t.CampaignItem.Campaign.Deleted;
                return false;
            }).ToList();
        }

        internal static IEnumerable<T> ExportableAwards<T>(IEnumerable<T> awards) where T : Award
        {
            return awards.Where(t =>
            {
                if (!t.CampaignItem.Deleted)
                    return !t.CampaignItem.Campaign.Deleted;
                return false;
            }).ToList();
        }

        public XElement ExportType(object entity)
        {
            var parentType = ActualTypeFrom(entity);
            var xelement = new XElement("Entity");
            foreach (var propertyInfo in parentType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!CanExportProperty(entity, propertyInfo))
                {
                    Log.Info("- skipping property {0}", (object) propertyInfo.Name);
                }
                else
                {
                    var propertyType = propertyInfo.PropertyType;
                    var obj1 = propertyInfo.GetValue(entity, null);
                    var propertyXml = new XElement("Property", NameAttribute(propertyInfo));
                    xelement.Add(propertyXml);
                    if (IsCollection(propertyType))
                    {
                        Log.Info("- exporting collection property {0}...", (object) propertyInfo.Name);
                        foreach (var obj2 in obj1 as IEnumerable)
                        {
                            var isoftDeletableEntity = obj2 as ISoftDeletableEntity;
                            if (isoftDeletableEntity == null || !isoftDeletableEntity.Deleted)
                            {
                                var iproperty = obj2 as IProperty;
                                if (iproperty != null)
                                {
                                    var definitionField = iproperty.GetDefinitionField();
                                    if (definitionField == null ||
                                        definitionField is ISoftDeletableEntity &&
                                        (definitionField as ISoftDeletableEntity).Deleted)
                                        continue;
                                }
                                ExportEntity(obj2, propertyXml, parentType, propertyInfo);
                            }
                        }
                        Log.Info("- collection property {0} completed", (object) propertyInfo.Name);
                    }
                    else if (IsComplexType(propertyType))
                    {
                        Log.Info("- exporting complex property {0}", (object) propertyInfo.Name);
                        if (obj1 != null)
                            ExportEntity(obj1, propertyXml, parentType, propertyInfo);
                    }
                    else
                    {
                        Log.Info("- exporting simple property {0}", (object) propertyInfo.Name);
                        if (propertyType == typeof (string) && _settings.CmsProvider != null)
                        {
                            if (IsMediaIdProperty(propertyInfo))
                            {
                                obj1 = _settings.CmsProvider.MediaIdToIdentifier(obj1 as string);
                                Log.Info("  - CMS provider translated property value to media identifier: {0}",
                                    obj1 ?? (object) "(null)");
                            }
                            else if (IsContentIdProperty(propertyInfo))
                            {
                                obj1 = _settings.CmsProvider.ContentIdToIdentifier(obj1 as string);
                                Log.Info("  - CMS provider translated property value to content identifier: {0}",
                                    obj1 ?? (object) "(null)");
                            }
                        }
                        propertyXml.Add(new XElement("Value", new XAttribute("isNull", (obj1 == null ? 1 : 0)),
                            obj1));
                    }
                }
            }
            return xelement;
        }

        private void ExportEntity(object item, XElement propertyXml, Type parentType, PropertyInfo parentProperty)
        {
            if (IsLocalEntityFor(item, parentType, parentProperty))
            {
                Log.Info("Exporting child entity {0}{1} ({2})",
                    item is IEntity
                        ? (object) string.Format("with ID {0}", (item as IEntity).Id)
                        : (object) string.Empty,
                    item is INamed
                        ? (object) string.Format(" ({0})", (item as INamed).Name)
                        : (object) string.Empty, (object) item.GetType());
                var xelement = ExportType(item);
                propertyXml.Add(xelement);
            }
            else
            {
                var entityIdentifier = GetEntityIdentifier(item);
                if (entityIdentifier == null)
                    return;
                var xelement = new XElement("EntityRef", new XAttribute("id", entityIdentifier));
                propertyXml.Add(xelement);
            }
        }

        public XDocument Serialize<T>(T source)
        {
            var xdocument = new XDocument();
            var xmlSerializer = new XmlSerializer(typeof (T));
            var writer = xdocument.CreateWriter();
            xmlSerializer.Serialize(writer, source);
            writer.Close();
            return xdocument;
        }
    }
}