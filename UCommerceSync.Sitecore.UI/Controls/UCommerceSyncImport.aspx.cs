using System;
using UCommerceSync.Logging;

namespace UCommerceSync.Sitecore.UI.Controls
{
    public partial class UCommerceSyncImport : PageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {

			inputLocation.Text = Helper.LocalSyncDataPath;

            importProductCatalogs.Enabled = importProductCatalogGroups.Checked;
            if (!importProductCatalogs.Enabled)
                importProductCatalogs.Checked = false;
            importProductCategories.Enabled = importProductCatalogs.Checked;
            if (!importProductCategories.Enabled)
                importProductCategories.Checked = false;
            importProducts.Enabled = importProductCategories.Checked;
            if (!importProducts.Enabled)
                importProducts.Checked = false;
            importProducts.Visible = Helper.CanImportProducts;
            CheckVersion();
            if (!Helper.HasImportFiles)
            {
                Warning("Error: No files found for import!");
                importButton.Enabled = false;
            }
            else
            {
                var ucommerceVersion = Helper.ImportFilesUCommerceVersion;
                if (!(ucommerceVersion != Helper.CurrentUCommerceVersion))
                    return;
                Warning(
                    string.Format(
                        "Warning: The for import were exported from uCommerce version {0} - the current uCommerce version is {1}.",
                        ucommerceVersion, Helper.CurrentUCommerceVersion));
            }
        }

        protected void importButton_Click(object sender, EventArgs e)
        {
            if (!Helper.HasImportFiles)
            {
                ImportStatus("No files found for import.");
            }
            else
            {
                var defaultLogger = new DefaultLogger();
                try
                {
                    new Importers.Importer(new ImportSettings
                    {
                        ImportProductCatalogGroups = importProductCatalogGroups.Checked,
                        ImportProductCatalogs = importProductCatalogs.Checked,
                        ImportProductCategories = importProductCategories.Checked,
                        ImportProducts = importProducts.Checked,
                        ImportMarketingFoundation = importMarketingFoundation.Checked,
                        PerformCleanUp = performCleanUp.Checked,
                        CmsProvider = new SitecoreProvider()
                    }, Helper.SyncDataPath, defaultLogger).Import();
                }
                catch (Exception ex)
                {
                    defaultLogger.Error(ex, "An unexpected error occurred during import");
                }
                if (defaultLogger.HasError)
                    ImportStatus(string.Format("An error occurred - see log file ({0}) for details.",
                        WriteLog("import error", defaultLogger)));
                else
                    ImportStatus("Imported completed.");
            }
        }

        private void ImportStatus(string message)
        {
            Status(importStatus, message);
        }

        private void Warning(string message)
        {
            warning.Text = message;
            warning.Visible = true;
        }
    }
}