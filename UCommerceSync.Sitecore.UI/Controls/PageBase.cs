using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using UCommerceSync.Logging;

namespace UCommerceSync.Sitecore.UI.Controls
{
    public class PageBase : Page
    {
        private static readonly Version MinUCommerceVersion = new Version(4, 0, 1, 13255);
        private static readonly Version MaxUCommerceVersion = new Version(6, 5, 0, 14311);
        protected Label versionWarning;

        protected string LocalSyncDataPath
        {
            get { return Helper.LocalSyncDataPath; }
        }

        protected void CheckVersion()
        {
            var ucommerceVersion = Helper.CurrentUCommerceVersion;
            if (!(ucommerceVersion < MinUCommerceVersion) && !(ucommerceVersion > MaxUCommerceVersion))
                return;
            versionWarning.Text =
                string.Format(
                    "uCommerceSync is only tested for use with uCommerce versions between {0} and {1}. The current version of uCommerce is {2}.",
                    MinUCommerceVersion, MaxUCommerceVersion, ucommerceVersion);
            versionWarning.Visible = true;
        }

        protected void Status(Label label, string message)
        {
            label.Text = message ?? string.Empty;
            label.Visible = label.Text.Length > 0;
        }

        protected string WriteLog(string name, ILogger log)
        {
            return Helper.WriteLog(name, log);
        }
    }
}