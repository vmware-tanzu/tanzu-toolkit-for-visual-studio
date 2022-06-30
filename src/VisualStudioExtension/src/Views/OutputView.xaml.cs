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
        public ICommand PauseOutputCommand { get; set; }
        public ICommand ResumeOutputCommand { get; set; }
        public ICommand StopProcessCommand { get; set; }
        public IViewModel ViewModel { get; private set; }

        public static readonly DependencyProperty _listItemMouseOverBrushProperty = DependencyProperty.Register("ListItemMouseOverBrush", typeof(Brush), typeof(OutputView), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty _windowButtonDownBorderBrushProperty = DependencyProperty.Register("WindowButtonDownBorderBrush", typeof(Brush), typeof(OutputView), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty _windowButtonDownHoverBrushProperty = DependencyProperty.Register("WindowButtonDownHoverBrush", typeof(Brush), typeof(OutputView), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty _windowPanelBrushProperty = DependencyProperty.Register("WindowPanelBrush", typeof(Brush), typeof(OutputView), new PropertyMetadata(default(Brush)));

        public Brush ListItemMouseOverBrush { get { return (Brush)GetValue(_listItemMouseOverBrushProperty); } set { SetValue(_listItemMouseOverBrushProperty, value); } }
        public Brush WindowButtonDownBorderBrush { get { return (Brush)GetValue(_windowButtonDownBorderBrushProperty); } set { SetValue(_windowButtonDownBorderBrushProperty, value); } }
        public Brush WindowButtonDownHoverBrush { get { return (Brush)GetValue(_windowButtonDownHoverBrushProperty); } set { SetValue(_windowButtonDownHoverBrushProperty, value); } }
        public Brush WindowPanelBrush { get { return (Brush)GetValue(_windowPanelBrushProperty); } set { SetValue(_windowPanelBrushProperty, value); } }

        public OutputView()
        {
            InitializeComponent();
        }

        public OutputView(IOutputViewModel viewModel, IThemeService themeService)
        {
            themeService.SetTheme(this);
            DataContext = viewModel;
            ViewModel = viewModel as IViewModel;

            bool alwaysTrue(object arg) { return true; }
            bool canPause(object arg) { return !viewModel.OutputPaused; }
            bool canResume(object arg) { return viewModel.OutputPaused; }

            ClearContentCommand = new DelegatingCommand(viewModel.ClearContent, alwaysTrue);
            PauseOutputCommand = new DelegatingCommand(viewModel.PauseOutput, canPause);
            ResumeOutputCommand = new DelegatingCommand(viewModel.ResumeOutput, canResume);

            InitializeComponent();
            autoScrollToggleBtn.IsChecked = true;
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
