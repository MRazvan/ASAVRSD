using System.Windows;
using System.Windows.Controls;

namespace AVR.Debugger.StackViewer.Controls
{
    public class TreeListView : TreeView
    {
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
