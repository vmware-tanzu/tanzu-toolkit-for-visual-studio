using Microsoft.VisualStudio.PlatformUI;
using System.Windows;

namespace Tanzu.Toolkit.VisualStudio.Views
{
    /// <summary>
    /// Interaction logic for SsoDialog.xaml
    /// </summary>
    public partial class SsoDialogView : DialogWindow, ISsoDialogView
    {
        public SsoDialogView()
        {
            InitializeComponent();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
