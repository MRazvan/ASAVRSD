using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace StackViewer.Controls
{
    public class TreeListViewItem : TreeViewItem
    {
        private int _level = -1;

        public int Level
        {
            get
            {
                if (this._level == -1)
                {
                    TreeListViewItem treeListViewItem = ItemsControl.ItemsControlFromItemContainer((DependencyObject)this) as TreeListViewItem;
                    this._level = treeListViewItem != null ? treeListViewItem.Level + 1 : 0;
                }
                return this._level;
            }
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return (DependencyObject)new TreeListViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeListViewItem;
        }
    }
}
