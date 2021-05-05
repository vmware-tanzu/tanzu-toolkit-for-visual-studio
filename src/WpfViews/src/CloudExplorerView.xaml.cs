﻿using System.Windows.Controls;
using System.Windows.Input;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.WpfViews.Commands;

namespace Tanzu.Toolkit.WpfViews
{
    /// <summary>
    /// Interaction logic for CloudExplorerView.xaml
    /// </summary>
    public partial class CloudExplorerView : UserControl, ICloudExplorerView
    {
        public ICommand OpenLoginFormCommand { get; }
        public ICommand StopCfAppCommand { get; }
        public ICommand StartCfAppCommand { get; }
        public ICommand DeleteCfAppCommand { get; }
        public ICommand DisplayRecentAppLogsCommand { get; }
        public ICommand RefreshSpaceCommand { get; }
        public ICommand RefreshAllCommand { get; }
        public ICommand RemoveCloudConnectionCommand { get; }

        public CloudExplorerView()
        {
            InitializeComponent();
        }

        public CloudExplorerView(ICloudExplorerViewModel viewModel)
        {
            OpenLoginFormCommand = new DelegatingCommand(viewModel.OpenLoginView, viewModel.CanOpenLoginView);
            StopCfAppCommand = new AsyncDelegatingCommand(viewModel.StopCfApp, viewModel.CanStopCfApp);
            StartCfAppCommand = new AsyncDelegatingCommand(viewModel.StartCfApp, viewModel.CanStartCfApp);
            DeleteCfAppCommand = new AsyncDelegatingCommand(viewModel.DeleteCfApp, viewModel.CanDeleteCfApp);
            DisplayRecentAppLogsCommand = new AsyncDelegatingCommand(viewModel.DisplayRecentAppLogs, viewModel.CanDisplayRecentAppLogs);
            RefreshSpaceCommand = new AsyncDelegatingCommand(viewModel.RefreshSpace, viewModel.CanRefreshSpace);
            RefreshAllCommand = new AsyncDelegatingCommand(viewModel.RefreshAllCloudConnections, viewModel.CanRefreshAllCloudConnections);
            RemoveCloudConnectionCommand = new DelegatingCommand(viewModel.RemoveCloudConnection, viewModel.CanRemoveCloudConnecion);

            DataContext = viewModel;
            InitializeComponent();
        }

    }
}