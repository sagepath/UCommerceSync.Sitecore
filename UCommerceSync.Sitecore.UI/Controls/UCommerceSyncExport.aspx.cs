using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UCommerceSync.Logging;

namespace UCommerceSync.Sitecore.UI.Controls
{
    public partial class UCommerceSyncExport : PageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            CheckVersion();
        }

        protected void btnExport_OnClick(object sender, EventArgs e)
        {
            var defaultLogger = new DefaultLogger();
            DownloadStatus(null);
            try
            {
                new Exporters.Exporter(new ExportSettings
                {
                    ExportProducts = cbExportProducts.Checked,
                    CmsProvider = new SitecoreProvider()
                }, Helper.SyncDataPath, defaultLogger).Export();
            }
            catch (Exception ex)
            {
                defaultLogger.Error(ex, "An unexpected error occurred during export");
            }
            if (defaultLogger.HasError)
            {
                ExportStatus(string.Format("An error occorred - see log file ({0}) for details.",
                    WriteLog("export error", defaultLogger)));
            }
            else
                ExportStatus("Export completed.");
        }

        protected void btnDownload_OnClick(object sender, EventArgs e)
        {
            ExportStatus(null);
            var files = new DirectoryInfo(Helper.SyncDataPath).GetFiles("*.xml");
            if (!files.Any())
            {
                DownloadStatus("No export files found for download.");
            }
            else
            {
                var xdocument = new XDocument(new XElement("uCommerceSync"));
                foreach (var fileInfo in files)
                {
                    var root = XDocument.Load(fileInfo.FullName).Root;
                    if (root == null)
                    {
                        DownloadStatus("Export file " + fileInfo.Name + " is empty.");
                        return;
                    }
                    var xelement = new XElement("Entities", new XAttribute("file", fileInfo.Name), root);
                    xdocument.Root.Add(xelement);
                }
                using (var memoryStream = new MemoryStream())
                {
                    xdocument.Save(memoryStream, SaveOptions.None);
                    var buffer = memoryStream.ToArray();
                    Response.Clear();
                    Response.AppendHeader("Content-Disposition",
                        "filename=uCommerceSync_" + DateTime.Now.ToString("yyyy_MM_dd_hh_mm") + ".xml");
                    Response.AppendHeader("Content-Length", buffer.Length.ToString());
                    Response.ContentType = "application/octet-stream";
                    Response.BinaryWrite(buffer);
                    Response.End();
                }
            }
            ;
        }

        private void ExportStatus(string message)
        {
            Status(exportStatus, message);
        }

        private void DownloadStatus(string message)
        {
            Status(downloadStatus, message);
        }
    }
}