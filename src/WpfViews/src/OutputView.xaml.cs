using System;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.WpfViews.Services;
using Tanzu.Toolkit.WpfViews.ThemeService;

namespace Tanzu.Toolkit.WpfViews
{
    /// <summary>
    /// Interaction logic for OutputView.xaml.
    /// </summary>
    public partial class OutputView : UserControl, IOutputView, IView
    {
        private IServiceProvider _services;

        public IViewModel ViewModel { get; private set; }

        public OutputView()
        {
            InitializeComponent();
        }

        public OutputView(IOutputViewModel viewModel, IServiceProvider services, IThemeService themeService)
        {
            _services = services;
            themeService.SetTheme(this);
            DataContext = viewModel;
            ViewModel = viewModel as IViewModel;
            InitializeComponent();
        }

        public void Show()
        {
            var viewService = _services.GetRequiredService<IViewService>();
            viewService.DisplayViewByType(GetType());
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
