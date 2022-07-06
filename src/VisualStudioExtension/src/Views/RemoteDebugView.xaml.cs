using Microsoft.VisualStudio.PlatformUI;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Tanzu.Toolkit.ViewModels.RemoteDebug;
using Tanzu.Toolkit.VisualStudio.Services;
using Tanzu.Toolkit.VisualStudio.Views;
using Tanzu.Toolkit.VisualStudio.Views.Commands;

namespace Tanzu.Toolkit.VisualStudio
{
    /// <summary>
    /// Interaction logic for RemoteDebugView.xaml
    /// </summary>
    public partial class RemoteDebugView : AbstractModal, IRemoteDebugView
    {
        public ICommand CancelCommand { get; }
        public ICommand OpenLoginViewCommand { get; }
        public ICommand ResolveMissingAppCommand { get; }
        public ICommand ShowDeploymentWindowCommand { get; }
        public Brush ListItemMouseOverBrush { get { return (Brush)GetValue(_listItemMouseOverBrushProperty); } set { SetValue(_listItemMouseOverBrushProperty, value); } }
        public Brush SelectedItemActiveBrush { get { return (Brush)GetValue(_selectedItemActiveBrushProperty); } set { SetValue(_selectedItemActiveBrushProperty, value); } }
        public Brush GridHeaderBrush { get { return (Brush)GetValue(_gridHeaderBrushProperty); } set { SetValue(_gridHeaderBrushProperty, value); } }

        public static readonly DependencyProperty _listItemMouseOverBrushProperty = DependencyProperty.Register("ListItemMouseOverBrush", typeof(Brush), typeof(RemoteDebugView), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty _selectedItemActiveBrushProperty = DependencyProperty.Register("SelectedItemActiveBrush", typeof(Brush), typeof(RemoteDebugView), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty _gridHeaderBrushProperty = DependencyProperty.Register("GridHeaderBrushProperty", typeof(Brush), typeof(RemoteDebugView), new PropertyMetadata(default(Brush)));

        public RemoteDebugView(IRemoteDebugViewModel viewModel, IThemeService themeService)
        {
            bool alwaysTrue(object arg) { return true; }

            themeService.SetTheme(this);
            DataContext = viewModel;
            CancelCommand = new DelegatingCommand(viewModel.Close, viewModel.CanCancel);
            OpenLoginViewCommand = new DelegatingCommand(viewModel.OpenLoginView, alwaysTrue);
            ResolveMissingAppCommand = new AsyncDelegatingCommand(viewModel.StartDebuggingAppAsync, viewModel.CanStartDebuggingApp);
            ShowDeploymentWindowCommand = new DelegatingCommand(viewModel.DisplayDeploymentWindow, viewModel.CanDisplayDeploymentWindow);
            MouseDown += Window_MouseDown;
            InitializeComponent();
        }
    }
}
