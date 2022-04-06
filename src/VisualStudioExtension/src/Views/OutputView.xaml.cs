using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.VisualStudio.Services;
using Tanzu.Toolkit.VisualStudio.Views.Commands;

namespace Tanzu.Toolkit.VisualStudio.Views
{
    /// <summary>
    /// Interaction logic for OutputView.xaml.
    /// </summary>
    public partial class OutputView : UserControl, IOutputView, IView
    {
        public ICommand ClearContentCommand { get; set; }
        public IViewModel ViewModel { get; private set; }

        public static readonly DependencyProperty ListItemMouseOverBrushProperty = DependencyProperty.Register("ListItemMouseOverBrush", typeof(Brush), typeof(OutputView), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty WindowButtonDownBorderBrushProperty = DependencyProperty.Register("WindowButtonDownBorderBrush", typeof(Brush), typeof(OutputView), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty WindowButtonDownHoverBrushProperty = DependencyProperty.Register("WindowButtonDownHoverBrush", typeof(Brush), typeof(OutputView), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty WindowPanelBrushProperty = DependencyProperty.Register("WindowPanelBrush", typeof(Brush), typeof(OutputView), new PropertyMetadata(default(Brush)));

        public Brush ListItemMouseOverBrush { get { return (Brush)GetValue(ListItemMouseOverBrushProperty); } set { SetValue(ListItemMouseOverBrushProperty, value); } }
        public Brush WindowButtonDownBorderBrush { get { return (Brush)GetValue(WindowButtonDownBorderBrushProperty); } set { SetValue(WindowButtonDownBorderBrushProperty, value); } }
        public Brush WindowButtonDownHoverBrush { get { return (Brush)GetValue(WindowButtonDownHoverBrushProperty); } set { SetValue(WindowButtonDownHoverBrushProperty, value); } }
        public Brush WindowPanelBrush { get { return (Brush)GetValue(WindowPanelBrushProperty); } set { SetValue(WindowPanelBrushProperty, value); } }

        public OutputView()
        {
            InitializeComponent();
        }

        public OutputView(IOutputViewModel viewModel, IServiceProvider services, IThemeService themeService)
        {
            bool alwaysTrue(object arg) { return true; }
            themeService.SetTheme(this);
            DataContext = viewModel;
            ViewModel = viewModel as IViewModel;
            ClearContentCommand = new DelegatingCommand(viewModel.ClearContent, alwaysTrue);
            InitializeComponent();
            autoScrollToggleBtn.IsChecked = true;
        }

        public void Show()
        {
            DisplayView?.Invoke();
        }

        /// <summary>
        /// This action starts null; the expectation is that VsToolWindowService will provide 
        /// a method which is able to display the tool window associated with this view.
        /// </summary>
        public Action DisplayView { get; set; }

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
            if ((bool)autoScrollToggleBtn.IsChecked)
            {
                OutputScrollViewer.ScrollToBottom();
            }
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            OutputScrollViewer.ScrollToBottom();
        }

        private void OutputScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollBarAtBottom = OutputScrollViewer.VerticalOffset == (OutputScrollViewer.ExtentHeight - OutputScrollViewer.ViewportHeight);
            autoScrollToggleBtn.IsChecked = scrollBarAtBottom;
        }
    }
}
