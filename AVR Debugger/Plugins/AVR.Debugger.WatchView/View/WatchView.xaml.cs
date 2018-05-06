using System.Windows;
using System.Windows.Controls;

namespace AVR.Debugger.WatchViewer.View
{
    /// <summary>
    ///     Interaction logic for LocalsView.xaml
    /// </summary>
    public partial class WatchView : UserControl
    {
        public WatchView()
        {
            InitializeComponent();
        }

        private void Peripheral_Expanded(object sender, RoutedEventArgs e)
        {
        }
    }
}