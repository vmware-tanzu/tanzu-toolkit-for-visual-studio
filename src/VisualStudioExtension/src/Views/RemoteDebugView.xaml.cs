﻿using Microsoft.VisualStudio.PlatformUI;
using System.Windows;
using System.Windows.Input;
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
