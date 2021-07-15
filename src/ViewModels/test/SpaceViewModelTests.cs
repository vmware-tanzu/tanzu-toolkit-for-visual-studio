using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class SpaceViewModelTests : ViewModelTestSupport
    {
        private SpaceViewModel _sut;
        private List<string> _receivedEvents;
        CloudExplorerViewModel _fakeCloudExplorerViewModel;
        CfInstanceViewModel _fakeCfInstanceViewModel;
        OrgViewModel _fakeOrgViewModel;

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();
            
            MockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances).Returns(new Dictionary<string, CloudFoundryInstance>());

            MockUiDispatcherService.Setup(mock => mock.
                RunOnUiThread(It.IsAny<Action>()))
                    .Callback<Action>(action =>
                    {
                        // Run whatever method is passed to MockUiDispatcherService.RunOnUiThread; do not delegate to the UI Dispatcher
                        action();
                    });

            _fakeCloudExplorerViewModel = new CloudExplorerViewModel(Services);
            _fakeCfInstanceViewModel = new CfInstanceViewModel(FakeCfInstance, _fakeCloudExplorerViewModel, Services, expanded: true);
            _fakeOrgViewModel = new OrgViewModel(FakeCfOrg, _fakeCfInstanceViewModel, _fakeCloudExplorerViewModel, Services);
            _sut = new SpaceViewModel(FakeCfSpace, _fakeOrgViewModel, _fakeCloudExplorerViewModel, Services);

            _receivedEvents = new List<string>();
            _sut.PropertyChanged += (sender, e) =>
            {
                _receivedEvents.Add(e.PropertyName);
            };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsDisplayTextToSpaceName()
        {
            Assert.AreEqual(FakeCfSpace.SpaceName, _sut.DisplayText);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsLoadingPlaceholder()
        {
            Assert.AreEqual(SpaceViewModel.LoadingMsg, _sut.LoadingPlaceholder.DisplayText);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsEmptyPlaceholder()
        {
            Assert.AreEqual(SpaceViewModel.EmptyAppsPlaceholderMsg, _sut.EmptyPlaceholder.DisplayText);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsParent()
        {
            Assert.AreEqual(_fakeOrgViewModel, _sut.Parent);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsParentCloudExplorer()
        {
            Assert.AreEqual(_fakeCloudExplorerViewModel, _sut.ParentCloudExplorer);
        }

        [TestMethod]
        public async Task LoadChildren_UpdatesAllSpaces()
        {
            var initialAppsList = new System.Collections.ObjectModel.ObservableCollection<TreeViewItemViewModel>
            {
                new AppViewModel(new CloudFoundryApp("initial app 1", null, null, null), Services),
                new AppViewModel(new CloudFoundryApp("initial app 2", null, null, null), Services),
                new AppViewModel(new CloudFoundryApp("initial app 3", null, null, null), Services),
            };

            var newAppsList = new List<CloudFoundryApp>
            {
                new CloudFoundryApp("initial app 1", null, null, null),
                new CloudFoundryApp("initial app 2", null, null, null),
            };
            var fakeAppsResult = new DetailedResult<List<CloudFoundryApp>>(
                succeeded: true,
                content: newAppsList,
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            _sut.Children = initialAppsList;

            /* erase record of initial "Children" event */
            _receivedEvents.Clear();

            MockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(_sut.Space, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeAppsResult);

            Assert.AreEqual(initialAppsList.Count, _sut.Children.Count);

            await _sut.LoadChildren();

            Assert.AreEqual(newAppsList.Count, _sut.Children.Count);

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("Children", _receivedEvents[0]);

            Assert.IsFalse(_sut.HasEmptyPlaceholder);
        }

        [TestMethod]
        public async Task LoadChildren_AssignsNoAppsPlaceholder_WhenThereAreNoApps()
        {
            var fakeAppsResult = new DetailedResult<List<CloudFoundryApp>>(
                succeeded: true,
                content: EmptyListOfApps,
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(_sut.Space, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeAppsResult);

            await _sut.LoadChildren();

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());
            Assert.AreEqual(SpaceViewModel.EmptyAppsPlaceholderMsg, _sut.Children[0].DisplayText);

            Assert.AreEqual(_sut.EmptyPlaceholder, _sut.Children[0]);
            Assert.IsTrue(_sut.HasEmptyPlaceholder);

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("Children", _receivedEvents[0]);
        }

        [TestMethod]
        public async Task LoadChildren_SetsIsLoadingToFalse_WhenComplete()
        {
            var fakeAppsResult = new DetailedResult<List<CloudFoundryApp>>(
                succeeded: true,
                content: EmptyListOfApps,
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(_sut.Space, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeAppsResult);

            _sut.IsLoading = true;

            await _sut.LoadChildren();

            Assert.IsFalse(_sut.IsLoading);
        }

        [TestMethod]
        public async Task FetchChildren_ReturnsListOfApps_WithoutUpdatingChildren()
        {
            var fakeAppsList = new List<CloudFoundryApp>
            {
                new CloudFoundryApp("fake app name 1", "fake app id 1", FakeCfSpace, null),
                new CloudFoundryApp("fake app name 2", "fake app id 2", FakeCfSpace, null),
            };

            var fakeAppsResult = new DetailedResult<List<CloudFoundryApp>>(
                succeeded: true,
                content: fakeAppsList,
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(_sut.Space, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeAppsResult);

            /* pre-check presence of placeholder */
            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());

            var apps = await _sut.FetchChildren();

            Assert.AreEqual(2, apps.Count);

            /* confirm presence of placeholder */
            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());

            // property changed events should not be raised
            Assert.AreEqual(0, _receivedEvents.Count);
        }

        [TestMethod]
        public async Task LoadChildren_DisplaysErrorDialog_WhenAppsRequestFails()
        {
            var fakeFailedResult = new DetailedResult<List<CloudFoundryApp>>(
                succeeded: false,
                content: null,
                explanation: "junk",
                cmdDetails: FakeFailureCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
              GetAppsForSpaceAsync(_sut.Space, true, It.IsAny<int>()))
                .ReturnsAsync(fakeFailedResult);

            await _sut.LoadChildren();

            Assert.IsFalse(_sut.IsLoading);

            MockErrorDialogService.Verify(mock => mock.
              DisplayErrorDialog(SpaceViewModel._getAppsFailureMsg, fakeFailedResult.Explanation),
                Times.Once);
        }

        [TestMethod]
        public async Task FetchChildren_DisplaysErrorDialog_WhenAppsRequestFails()
        {
            var fakeFailedResult = new DetailedResult<List<CloudFoundryApp>>(
                succeeded: false,
                content: null,
                explanation: "junk",
                cmdDetails: FakeFailureCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
              GetAppsForSpaceAsync(_sut.Space, true, It.IsAny<int>()))
                .ReturnsAsync(fakeFailedResult);

            var result = await _sut.FetchChildren();

            CollectionAssert.AreEqual(EmptyListOfApps, result);

            MockErrorDialogService.Verify(mock => mock.
              DisplayErrorDialog(SpaceViewModel._getAppsFailureMsg, fakeFailedResult.Explanation),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("RefreshChildren")]
        public async Task RefreshSpace_UpdatesChildrenOnSpaceViewModel()
        {
            var fakeAppName1 = "fake app 1";
            var fakeAppName2 = "fake app 2";
            var fakeAppName3 = "fake app 3";
            var fakeAppName4 = "fake app 4";

            var fakeAppGuid1 = "fake app 1";
            var fakeAppGuid2 = "fake app 2";
            var fakeAppGuid3 = "fake app 3";
            var fakeAppGuid4 = "fake app 4";

            var initialState1 = "junk";
            var initialState2 = "asdf";
            var initialState3 = "xkcd";

            _sut.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                // to be removed:
                new AppViewModel(new CloudFoundryApp(fakeAppName1, fakeAppGuid1, _sut.Space, initialState1), Services), 
                // to stay:
                new AppViewModel(new CloudFoundryApp(fakeAppName2, fakeAppGuid2, _sut.Space, state: initialState2), Services), // should keep state after refresh 
                new AppViewModel(new CloudFoundryApp(fakeAppName3, fakeAppGuid3, _sut.Space, state: initialState3), Services), // should change state after refresh 
            };

            var fakeAppsList = new List<CloudFoundryApp>
            {
                // original:
                new CloudFoundryApp(fakeAppName2, fakeAppGuid2, _sut.Space, initialState2),
                new CloudFoundryApp(fakeAppName3, fakeAppGuid3, _sut.Space, "new state"),
                // new:
                new CloudFoundryApp(fakeAppName4, fakeAppGuid4, _sut.Space, "new app, new state"),
            };

            var fakeSuccessResult = new DetailedResult<List<CloudFoundryApp>>(succeeded: true, content: fakeAppsList);

            Assert.AreEqual(3, _sut.Children.Count);
            AppViewModel initialChildApp1 = (AppViewModel)_sut.Children[0];
            AppViewModel initialChildApp2 = (AppViewModel)_sut.Children[1];
            AppViewModel initialChildApp3 = (AppViewModel)_sut.Children[2];
            Assert.AreEqual(fakeAppName1, initialChildApp1.App.AppName);
            Assert.AreEqual(fakeAppName2, initialChildApp2.App.AppName);
            Assert.AreEqual(fakeAppName3, initialChildApp3.App.AppName);
            Assert.AreEqual(initialState2, initialChildApp2.App.State);
            Assert.AreEqual(initialState3, initialChildApp3.App.State);

            MockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(_sut.Space, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeSuccessResult);

            _receivedEvents.Clear();

            await _sut.RefreshChildren();

            Assert.AreEqual(3, _sut.Children.Count);
            AppViewModel refreshedChildApp1 = (AppViewModel)_sut.Children[0];
            AppViewModel refreshedChildApp2 = (AppViewModel)_sut.Children[1];
            AppViewModel refreshedChildApp3 = (AppViewModel)_sut.Children[2];
            Assert.AreEqual(fakeAppName2, refreshedChildApp1.App.AppName);
            Assert.AreEqual(fakeAppName3, refreshedChildApp2.App.AppName);
            Assert.AreEqual(fakeAppName4, refreshedChildApp3.App.AppName);
            Assert.AreEqual(initialState2, refreshedChildApp1.App.State); // previous state shouldn't have changed
            Assert.AreNotEqual(initialState3, refreshedChildApp2.App.State); // previous state should have changed

            // property changed event should be raised once for Children (update UI) & twice for IsRefreshing (set true when starting, false when done)
            Assert.AreEqual(3, _receivedEvents.Count);
            Assert.AreEqual("IsRefreshing", _receivedEvents[0]);
            Assert.AreEqual("Children", _receivedEvents[1]);
            Assert.AreEqual("IsRefreshing", _receivedEvents[2]);

            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("RefreshChildren")]
        public async Task RefreshChildren_AddsPlaceholder_WhenAllAppsAreRemoved()
        {
            var fakeInitialApp = new CloudFoundryApp("fake app name", "fake app id", _sut.Space, null);
            var avm = new AppViewModel(fakeInitialApp, Services);

            var fakeNoAppsResult = new DetailedResult<List<CloudFoundryApp>>(
                succeeded: true,
                content: new List<CloudFoundryApp>(), // simulate space having lost all apps before refresh
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(_sut.Space, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeNoAppsResult);

            _sut.Children = new ObservableCollection<TreeViewItemViewModel> { avm }; // simulate space initially having 1 app child

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(AppViewModel), _sut.Children[0].GetType());

            await _sut.RefreshChildren();

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());
            Assert.AreEqual(SpaceViewModel.EmptyAppsPlaceholderMsg, _sut.Children[0].DisplayText);
        }

        [TestMethod]
        [TestCategory("RefreshChildren")]
        public async Task RefreshChildren_RemovesPlaceholder_WhenEmptySpaceGainsChildren()
        {
            // simulate space initially having no app children
            _sut.Children = new ObservableCollection<TreeViewItemViewModel> { _sut.EmptyPlaceholder };
            _sut.HasEmptyPlaceholder = true;

            var fakeNewApp = new CloudFoundryApp("fake app name", "fake app id", _sut.Space, null);
            var avm = new AppViewModel(fakeNewApp, Services);

            var fakeSuccessfulAppsResult = new DetailedResult<List<CloudFoundryApp>>(
                succeeded: true,
                content: new List<CloudFoundryApp>
                {
                    fakeNewApp, // simulate space having gained an app child before refresh
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(_sut.Space, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeSuccessfulAppsResult);

            _sut.Children = new ObservableCollection<TreeViewItemViewModel> { _sut.EmptyPlaceholder }; // simulate space initially having no app children

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());
            Assert.AreEqual(SpaceViewModel.EmptyAppsPlaceholderMsg, _sut.Children[0].DisplayText);

            await _sut.RefreshChildren();

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(AppViewModel), _sut.Children[0].GetType());
        }

        [TestMethod]
        [TestCategory("RefreshChildren")]
        public async Task RefreshChildren_UpdatesAppState_ForAllChildren()
        {
            var initialState1 = "INITIAL_FAKE_STATE";
            var initialState2 = "INITIAL_JUNK_STATE";
            var initialState3 = "INITIAL_BOGUS_STATE";

            var fakeInitialApp1 = new CloudFoundryApp("fakeApp1", "junk", _sut.Space, initialState1);
            var fakeInitialApp2 = new CloudFoundryApp("fakeApp2", "junk", _sut.Space, initialState2);
            var fakeInitialApp3 = new CloudFoundryApp("fakeApp3", "junk", _sut.Space, initialState3);

            var fakeFreshApp1 = new CloudFoundryApp("fakeApp1", "junk", _sut.Space, initialState1);
            var fakeFreshApp2 = new CloudFoundryApp("fakeApp2", "junk", _sut.Space, "NEW_FRESH_STATE"); // simulate state change
            var fakeFreshApp3 = new CloudFoundryApp("fakeApp3", "junk", _sut.Space, "NEW_COOL_STATE"); // simulate state change

            _sut.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new AppViewModel(fakeInitialApp1, Services),
                new AppViewModel(fakeInitialApp2, Services),
                new AppViewModel(fakeInitialApp3, Services),
            };

            var fakeSuccessfulAppsResult = new DetailedResult<List<CloudFoundryApp>>(
                succeeded: true,
                content: new List<CloudFoundryApp>
                {
                    fakeFreshApp1,
                    fakeFreshApp2,
                    fakeFreshApp3,
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetAppsForSpaceAsync(_sut.Space, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeSuccessfulAppsResult);

            await _sut.RefreshChildren();

            Assert.AreEqual(fakeInitialApp1.State, fakeFreshApp1.State);
            Assert.AreNotEqual(fakeInitialApp2.State, fakeFreshApp2.State);
            Assert.AreNotEqual(fakeInitialApp3.State, fakeFreshApp3.State);
        }
    }
}
