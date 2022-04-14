﻿using Microsoft.VisualStudio.PlatformUI;
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
    public partial class RemoteDebugView : DialogWindow, IRemoteDebugView
    {
        public ICommand CancelCommand { get; }
        public ICommand OpenLoginViewCommand { get; }
        public ICommand ResolveMissingAppCommand { get; }
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
            CancelCommand = new DelegatingCommand(viewModel.Close, alwaysTrue);
            OpenLoginViewCommand = new DelegatingCommand(viewModel.OpenLoginView, alwaysTrue);
            ResolveMissingAppCommand = new AsyncDelegatingCommand(viewModel.ResolveMissingAppAsync, viewModel.CanResolveMissingApp);
            MouseDown += Window_MouseDown;
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
