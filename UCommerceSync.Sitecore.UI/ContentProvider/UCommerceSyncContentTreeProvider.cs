using System;
using System.Collections.Generic;
using UCommerce.Tree;
using UCommerce.Tree.Impl;

namespace UCommerceSync.Sitecore.UI.ContentProvider
{
    public class UCommerceSyncContentTreeProvider : ITreeContentProvider
    {
        public IList<ITreeNodeContent> GetChildren(string nodeType, string id)
        {
            switch (nodeType)
            {
                case UCommerce.Constants.DataProvider.NodeType.Root:
                    return BuildUCommerceSyncRoot();
                case Constants.UcommerceSync:
                    return BuildUCommerceSyncChildren();
                default:
                    throw new NotSupportedException(nodeType);
            }                        
        }        

        private IList<ITreeNodeContent> BuildUCommerceSyncRoot()
        {
            var result = new List<ITreeNodeContent>();
            result.Add(new TreeNodeContent(Constants.UcommerceSync, 100)
            {
                HasChildren = true,
                Name = "uCommerceSync",
                Icon = "uCommerceSync_Sync.png"
            });

            return result;
        }

        private IList<ITreeNodeContent> BuildUCommerceSyncChildren()
        {
            var result = new List<ITreeNodeContent>();
            result.Add(new TreeNodeContent(Constants.UcommerceSyncExport, 101, 100)
            {
                Name = "Export",
                Icon = "uCommerceSync_Export.png",
                HasChildren = false
            });
            result.Add(new TreeNodeContent(Constants.UcommerceSyncImport, 102, 100)
            {
                Name = "Import",
                Icon = "uCommerceSync_Import.png",
                HasChildren = false
            });

            return result;
        }

        public bool Supports(string nodeType)
        {
            return nodeType == UCommerce.Constants.DataProvider.NodeType.Root || nodeType == Constants.UcommerceSync;
        }
    }
}