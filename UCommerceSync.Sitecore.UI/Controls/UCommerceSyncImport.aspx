<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="UCommerceSyncImport.aspx.cs" Inherits="UCommerceSync.Sitecore.UI.Controls.UCommerceSyncImport" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <h1>uCommerceSync Import</h1>
        <div>
            <h4><asp:Label runat="server" ID="versionWarning"></asp:Label></h4>            
            <h4><asp:Label runat="server" ID="importStatus"></asp:Label></h4>
            <h4><asp:Label runat="server" ID="warning"></asp:Label></h4>
        </div>
        <table>
            <tr>
                <td style="width: 30%;vertical-align: top">
                    Import
                </td>
                <td>
                    <div>Import is performed from ~/App_Data/UCommerceSync/.</div>
                    <ul style="list-style: none">
                        <li>
                            <asp:CheckBox runat="server" ID="importProductCatalogGroups" AutoPostBack="true"/> Import stores
                        </li>
                        <li>
                            <asp:CheckBox runat="server" ID="importProductCatalogs" AutoPostBack="true"/> Import product catalogs
                        </li>
                        <li>
                            <asp:CheckBox runat="server" ID="importProductCategories" AutoPostBack="true"/> Import product categories
                        </li>
                        <li>
                            <asp:CheckBox runat="server" ID="importProducts" AutoPostBack="true"/> Import products
                        </li>
                        <li>
                            <asp:CheckBox runat="server" ID="importMarketingFoundation" AutoPostBack="true"/> Import marketing foundation
                        </li>
                        <li>
                            <asp:CheckBox runat="server" ID="performCleanUp" AutoPostBack="true"/> Delete items not found in import files
                        </li>
                    </ul>
                    <asp:Button runat="server" Text="Import" ID="importButton" OnClick="importButton_Click"/>
                </td>
            </tr>
        </table>
    </form>
</body>
</html>
