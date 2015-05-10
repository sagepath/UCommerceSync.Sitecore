using System;

namespace UCommerceSync.Sitecore.UI.Controls
{
    public partial class UCommerceSync : PageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            CheckVersion();
        }
    }
}