<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="UCommerceSyncExport.aspx.cs" Inherits="UCommerceSync.Sitecore.UI.Controls.UCommerceSyncExport" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <h1>uCommerceSync Export</h1>
        <div>
            <h4><asp:Label runat="server" ID="versionWarning"></asp:Label></h4>
            <h4><asp:Label runat="server" ID="exportStatus"></asp:Label></h4>
            <h4><asp:Label runat="server" ID="downloadStatus"></asp:Label></h4>
        </div>
        <div>            
            <table>
                <tr>
                    <td style="width: 30%;vertical-align: top">Export</td>
                    <td style="width: 70%;vertical-align: top">
                        <div>Export output files are saved to <asp:Label runat="server" ID="outputLocation"></asp:Label>.</div>
                        Export products <asp:CheckBox runat="server" ID="cbExportProducts"/><br/>
                        <asp:Button runat="server" ID="btnExport" Text="Exports" OnClick="btnExport_OnClick"/>
                    </td>
                </tr>
                <tr>
                    <td>Download</td>
                    <td>
                        <div>Click below to download all exported output files in a single file.</div>
                        <asp:Button runat="server" ID="btnDownload" Text="Download" OnClick="btnDownload_OnClick"/>
                    </td>
                </tr>
            </table>
        </div>
    </form>
</body>
</html>
