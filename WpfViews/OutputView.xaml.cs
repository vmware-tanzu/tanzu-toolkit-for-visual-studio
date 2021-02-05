using System.Windows.Controls;
using Tanzu.Toolkit.VisualStudio.ViewModels;

namespace Tanzu.Toolkit.VisualStudio.WpfViews
{
    /// <summary>
    /// Interaction logic for OutputView.xaml
    /// </summary>
    public partial class OutputView : UserControl, IOutputView, IView
    {
        public IViewModel ViewModel { get; private set; }

        public OutputView()
        {
            InitializeComponent();
        }

        public OutputView(IOutputViewModel viewModel)
        {
            DataContext = viewModel;
            ViewModel = viewModel as IViewModel;
            InitializeComponent();
        }

        /// <summary>
        /// If scroll viewer is already scrolled all the way down, scroll to
        /// bottom after printing new line of content.
        /// Otherwise, if scroll viewer is scrolled up more than 1 pixel above 
        /// the bottom of the scrollable content, do not scroll.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var offset = OutputScrollViewer.VerticalOffset;

            if (offset == (OutputScrollViewer.ExtentHeight - OutputScrollViewer.ViewportHeight))
            {
                OutputScrollViewer.ScrollToBottom();
            }

        }
    }
}
