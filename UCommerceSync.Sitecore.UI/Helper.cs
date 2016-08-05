using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using UCommerceSync.Logging;
using UCommerceSync.Sitecore.Exporters;

namespace UCommerceSync.Sitecore.UI
{
    internal class Helper
    {

		private static readonly string _localSyncDataPath = WebConfigurationManager.AppSettings["UcommerceSync.DataPath"] ?? "~/App_Data/UCommerceSync/";

        public static string LocalSyncDataPath
        {
            get { return _localSyncDataPath; }
        }

        public static string LocalLogsPath
        {
            get
            {
	            return string.Format("{0}{1}Logs/", LocalSyncDataPath, LocalSyncDataPath.EndsWith("/") ? string.Empty : "/");
            }
        }

        public static string SyncDataPath
        {
            get
            {
	            var directoryInfo = GetDirectoryInfo(LocalSyncDataPath);
                return directoryInfo.FullName;
            }
        }

	    private static DirectoryInfo GetDirectoryInfo(string path)
	    {
		    string pathToDir = path;
		    if (!System.IO.Path.IsPathRooted(path))
		    {
			    pathToDir = HttpContext.Current.Server.MapPath(path);
		    }

		    DirectoryInfo directoryInfo = new DirectoryInfo(pathToDir);
                if (!directoryInfo.Exists)
                    directoryInfo.Create();

		    return directoryInfo;
	    }

        public static string LogsPath
        {
            get
            {
                var directoryInfo = GetDirectoryInfo(LocalSyncDataPath);
                return directoryInfo.FullName;
            }
        }

        public static Version CurrentUCommerceVersion
        {
            get { return UCommerceSync.ImportExportBase.CurrentUCommerceVersion(); }
        }

        public static Version ImportFilesUCommerceVersion
        {
            get
            {
                var fileInfo = GetImportFiles().FirstOrDefault();
                if (fileInfo == null)
                    return new Version(0, 0, 0, 0);
                return ExporterHelper.GetUCommerceVersionFrom(fileInfo.FullName);
            }
        }

        public static bool CanImportProducts
        {
            get
            {
                var importFiles = GetImportFiles();
                var fileInfo1 = importFiles.FirstOrDefault(f => f.Name.ToLowerInvariant() == "product.xml");
                if (fileInfo1 == null)
                    return false;
                var fileInfo2 = importFiles.FirstOrDefault(f => f.Name.ToLowerInvariant() == "currency.xml");
                if (fileInfo2 == null ||
                    !(ExporterHelper.GetUCommerceVersionFrom(fileInfo1.FullName) ==
                      ExporterHelper.GetUCommerceVersionFrom(fileInfo2.FullName)))
                    return false;
                return ExporterHelper.GetTimestampFrom(fileInfo1.FullName) ==
                       ExporterHelper.GetTimestampFrom(fileInfo2.FullName);
            }
        }

        public static bool HasImportFiles
        {
            get { return GetImportFiles().Any(); }
        }

        private static IEnumerable<FileInfo> GetImportFiles()
        {
            return new DirectoryInfo(SyncDataPath).GetFiles("*.xml");
        }

        public static string WriteLog(string name, ILogger log)
        {
            var path2 = string.Format("{0} {1}.log", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"), name);
            File.WriteAllText(Path.Combine(LogsPath, path2), log.Content);
            return Path.Combine(LocalLogsPath, path2);
        }
    }
}