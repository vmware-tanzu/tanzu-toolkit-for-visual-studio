using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        CfInstanceViewModel _fakeCfInstanceViewModel;
        OrgViewModel _fakeOrgViewModel;
        private CloudFoundrySpace _expectedSpace;
        private readonly bool _expectedSkipSslValue = false;
        private readonly int _expectedRetryAmount = 1;

        private readonly DetailedResult<List<CloudFoundryApp>> _fakeAppsResponse = new()
        {
            Succeeded = true,
            Content =
            [
                _fakeApps[0],
                _fakeApps[1],
                _fakeApps[2]
            ]
        };

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            _fakeCfInstanceViewModel = new CfInstanceViewModel(_fakeCfInstance, MockTanzuExplorerViewModel.Object, Services, expanded: true);

            MockUiDispatcherService.Setup(mock => mock.RunOnUIThreadAsync(It.IsAny<Action>()))
                .Callback<Action>(action =>
                {
                    // Run whatever method is passed to MockUiDispatcherService.RunOnUiThread; do not delegate to the UI Dispatcher
                    action();
                });

            MockThreadingService.Setup(m => m
                    .RemoveItemFromCollectionOnUIThreadAsync(It.IsAny<ObservableCollection<TreeViewItemViewModel>>(), It.IsAny<TreeViewItemViewModel>()))
                .Callback<ObservableCollection<TreeViewItemViewModel>, TreeViewItemViewModel>((collection, item) => { collection.Remove(item); });

            MockThreadingService.Setup(m => m
                    .AddItemToCollectionOnUIThreadAsync(It.IsAny<ObservableCollection<TreeViewItemViewModel>>(), It.IsAny<TreeViewItemViewModel>()))
                .Callback<ObservableCollection<TreeViewItemViewModel>, TreeViewItemViewModel>((collection, item) => { collection.Add(item); });

            MockTanzuExplorerViewModel.SetupGet(m => m.CloudFoundryConnection).Returns(_fakeCfInstanceViewModel);

            _fakeCfInstanceViewModel = new CfInstanceViewModel(_fakeCfInstance, MockTanzuExplorerViewModel.Object, Services, expanded: true);
            _fakeOrgViewModel = new OrgViewModel(_fakeCfOrg, _fakeCfInstanceViewModel, MockTanzuExplorerViewModel.Object, Services);
            _sut = new SpaceViewModel(_fakeCfSpace, _fakeOrgViewModel, MockTanzuExplorerViewModel.Object, Services, expanded: true);

            _receivedEvents = [];
            _sut.PropertyChanged += (sender, e) => { _receivedEvents.Add(e.PropertyName); };

            _expectedSpace = _sut.Space;
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
            Assert.AreEqual(_fakeCfSpace.SpaceName, _sut.DisplayText);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsLoadingPlaceholder()
        {
            Assert.AreEqual(SpaceViewModel._loadingMsg, _sut.LoadingPlaceholder.DisplayText);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsEmptyPlaceholder()
        {
            Assert.AreEqual(SpaceViewModel._emptyAppsPlaceholderMsg, _sut.EmptyPlaceholder.DisplayText);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsParent()
        {
            Assert.AreEqual(_fakeOrgViewModel, _sut.Parent);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsParentTanzuExplorer()
        {
            Assert.AreEqual(MockTanzuExplorerViewModel.Object, _sut.ParentTanzuExplorer);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildrenAsync")]
        public async Task UpdateAllChildren_RemovesStaleChildrenOnUiThread_WhenAppsRequestSucceeds()
        {
            /** mock 4 initial children */
            _sut.Children =
            [
                new AppViewModel(_fakeApps[0], Services),
                new AppViewModel(_fakeApps[1], Services),
                new AppViewModel(_fakeApps[2], Services),
                new AppViewModel(_fakeApps[3], Services)
            ];

            /** mock retrieving all initial children except for FakeApps[3] */
            MockCloudFoundryService.Setup(m => m
                    .GetAppsForSpaceAsync(_expectedSpace, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(_fakeAppsResponse);

            await _sut.UpdateAllChildrenAsync();

            Assert.HasCount(3, _sut.Children);
            Assert.IsTrue(_sut.Children.Any(child => child is AppViewModel app && app.App.AppName == _fakeApps[0].AppName));
            Assert.IsTrue(_sut.Children.Any(child => child is AppViewModel app && app.App.AppName == _fakeApps[1].AppName));
            Assert.IsTrue(_sut.Children.Any(child => child is AppViewModel app && app.App.AppName == _fakeApps[2].AppName));
            Assert.IsFalse(_sut.Children.Any(child => child is AppViewModel app && app.App.AppName == _fakeApps[3].AppName));

            MockThreadingService.Verify(m => m
                    .RemoveItemFromCollectionOnUIThreadAsync(_sut.Children, It.Is<AppViewModel>((ovm) => ovm.App == _fakeApps[3])),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildrenAsync")]
        public async Task UpdateAllChildren_AddsNewChildrenOnUiThread_WhenAppsRequestSucceeds()
        {
            /** mock 2 initial children */
            _sut.Children =
            [
                new AppViewModel(_fakeApps[0], Services),
                new AppViewModel(_fakeApps[1], Services)
            ];

            /** mock retrieving all initial children plus FakeApps[2] */
            MockCloudFoundryService.Setup(m => m
                    .GetAppsForSpaceAsync(_expectedSpace, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(_fakeAppsResponse);

            await _sut.UpdateAllChildrenAsync();

            Assert.HasCount(3, _sut.Children);
            Assert.IsTrue(_sut.Children.Any(child => child is AppViewModel app && app.App.AppName == _fakeApps[0].AppName));
            Assert.IsTrue(_sut.Children.Any(child => child is AppViewModel app && app.App.AppName == _fakeApps[1].AppName));
            Assert.IsTrue(_sut.Children.Any(child => child is AppViewModel app && app.App.AppName == _fakeApps[2].AppName));

            MockThreadingService.Verify(m => m
                    .AddItemToCollectionOnUIThreadAsync(_sut.Children, It.Is<AppViewModel>((ovm) => ovm.App == _fakeApps[2])),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildrenAsync")]
        public async Task UpdateAllChildren_RefreshesAppStateOnAllChildren_WhenAppsRequestSucceeds()
        {
            const string oldStateExpectedToChange = "an old state which should get updated";

            /** mock 4 initial children */
            var initialChildren = new ObservableCollection<TreeViewItemViewModel>
            {
                new AppViewModel(_fakeApps[0], Services),
                new AppViewModel(new CloudFoundryApp(_fakeApps[1].AppName, _fakeApps[1].AppId, _fakeApps[1].ParentSpace, oldStateExpectedToChange), Services),
                new AppViewModel(_fakeApps[3], Services),
                new AppViewModel(_fakeApps[4], Services)
            };
            _sut.Children = new ObservableCollection<TreeViewItemViewModel>(initialChildren);

            /** mock 3 children in response:
             * first (FakeApps[0]) is idential to initial: FakeApps[0]
             * second (FakeApps[1]) is same as initial, just with a different state than it had initially
             * third (FakeApps[2]) is new
             * initial children FakeApps[3] & FakeApps[4] have been lost
             */
            MockCloudFoundryService.Setup(m => m
                    .GetAppsForSpaceAsync(_expectedSpace, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(_fakeAppsResponse);

            MockThreadingService.Setup(m => m
                    .StartBackgroundTaskAsync(It.IsAny<Func<Task>>()))
                .Verifiable();

            await _sut.UpdateAllChildrenAsync();

            Assert.HasCount(3, _sut.Children);

            // ensure app didn't change
            var firstOldApp = initialChildren[0] as AppViewModel;
            var firstFreshApp = _sut.Children[0] as AppViewModel;
            Assert.AreEqual(firstFreshApp.App.AppId, firstOldApp.App.AppId);
            Assert.AreEqual(firstFreshApp.App.State, firstOldApp.App.State);

            // ensure app state was updated
            var secondFreshApp = _sut.Children[1] as AppViewModel;
            var secondOldApp = initialChildren[1] as AppViewModel;
            Assert.AreEqual(secondFreshApp.App.AppId, secondOldApp.App.AppId);
            Assert.AreNotEqual(oldStateExpectedToChange, secondFreshApp.App.State);

            // ensure app was added
            var thirdFreshApp = _sut.Children[2] as AppViewModel;
            Assert.IsFalse(initialChildren.Any(a => a is AppViewModel avm && avm.App.AppId == thirdFreshApp.App.AppId));

            // ensure app was removed
            var thirdOldApp = initialChildren[2] as AppViewModel;
            Assert.IsFalse(_sut.Children.Any(a => a is AppViewModel avm && avm.App.AppId == thirdOldApp.App.AppId));

            // ensure app was removed
            var fourthOldApp = initialChildren[3] as AppViewModel;
            Assert.IsFalse(_sut.Children.Any(a => a is AppViewModel avm && avm.App.AppId == fourthOldApp.App.AppId));
        }

        [TestMethod]
        [TestCategory("UpdateAllChildrenAsync")]
        public async Task UpdateAllChildren_AssignsEmptyPlaceholder_WhenAppsRequestReturnsNoApps()
        {
            _fakeAppsResponse.Content.Clear();

            var initialChildren = new ObservableCollection<TreeViewItemViewModel> { new AppViewModel(_fakeApps[0], Services), new AppViewModel(_fakeApps[1], Services) };
            _sut.Children = new ObservableCollection<TreeViewItemViewModel>(initialChildren);

            MockCloudFoundryService.Setup(m => m
                    .GetAppsForSpaceAsync(_expectedSpace, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(_fakeAppsResponse);

            Assert.IsFalse(_sut.Children.Any(child => child is PlaceholderViewModel));

            await _sut.UpdateAllChildrenAsync();

            Assert.HasCount(1, _sut.Children);
            Assert.IsTrue(_sut.Children[0].Equals(_sut.EmptyPlaceholder));
            foreach (var child in initialChildren)
            {
                MockThreadingService.Verify(m => m.RemoveItemFromCollectionOnUIThreadAsync(_sut.Children, child), Times.Once);
            }

            MockThreadingService.Verify(m => m.AddItemToCollectionOnUIThreadAsync(_sut.Children, _sut.EmptyPlaceholder), Times.Once);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildrenAsync")]
        public async Task UpdateAllChildren_SetsIsLoadingTrueAtStart_AndSetsIsLoadingFalseAtEnd()
        {
            MockCloudFoundryService.Setup(m => m.GetAppsForSpaceAsync(_expectedSpace, _expectedSkipSslValue, _expectedRetryAmount))
                .Callback(() =>
                {
                    // ensure IsLoading was set to true by the time apps were queried
                    Assert.IsTrue(_sut.IsLoading);
                }).ReturnsAsync(_fakeAppsResponse);

            Assert.IsFalse(_sut.IsLoading);
            Assert.IsTrue(_sut.IsExpanded);

            await _sut.UpdateAllChildrenAsync();

            Assert.IsFalse(_sut.IsLoading);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildrenAsync")]
        public async Task UpdateAllChildren_RemovesEmptyPlaceholder_WhenAppsRequestReturnsApps()
        {
            _sut.Children =
            [
                _sut.EmptyPlaceholder
            ];

            MockCloudFoundryService.Setup(m => m
                    .GetAppsForSpaceAsync(_expectedSpace, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(_fakeAppsResponse);

            Assert.HasCount(1, _sut.Children);
            Assert.IsTrue(_sut.Children[0].Equals(_sut.EmptyPlaceholder));

            await _sut.UpdateAllChildrenAsync();

            Assert.HasCount(3, _sut.Children);
            Assert.IsFalse(_sut.Children.Any(child => child is PlaceholderViewModel));
        }

        [TestMethod]
        [TestCategory("UpdateAllChildrenAsync")]
        public async Task UpdateAllChildren_RemovesLoadingPlaceholder_WhenAppsRequestReturnsApps()
        {
            _fakeAppsResponse.Content.Clear();

            _sut.Children =
            [
                new AppViewModel(_fakeApps[0], Services),
                new AppViewModel(_fakeApps[1], Services)
            ];

            MockCloudFoundryService.Setup(m => m
                    .GetAppsForSpaceAsync(_expectedSpace, _expectedSkipSslValue, _expectedRetryAmount))
                .Callback(() =>
                {
                    // ensure loading placeholder present at time of loading fresh children
                    Assert.IsTrue(_sut.IsLoading);
                    Assert.Contains(_sut.LoadingPlaceholder, _sut.Children);
                }).ReturnsAsync(_fakeAppsResponse);

            await _sut.UpdateAllChildrenAsync();

            Assert.DoesNotContain(_sut.LoadingPlaceholder, _sut.Children);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildrenAsync")]
        public async Task UpdateAllChildren_CollapsesSelf_AndSetsAuthRequiredTrue_WhenAppsRequestFailsWithInvalidRefreshToken()
        {
            var fakeInvalidTokenResponse = new DetailedResult<List<CloudFoundryApp>> { Succeeded = false, Explanation = "junk", FailureType = FailureType.InvalidRefreshToken };

            MockCloudFoundryService.Setup(m => m
                    .GetAppsForSpaceAsync(_expectedSpace, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(fakeInvalidTokenResponse);
            MockTanzuExplorerViewModel.SetupSet(m => m.AuthenticationRequired = true)
                .Verifiable();

            Assert.IsTrue(_sut.IsExpanded);
            MockTanzuExplorerViewModel.VerifySet(m => m.AuthenticationRequired = true, Times.Never);

            await _sut.UpdateAllChildrenAsync();

            Assert.IsFalse(_sut.IsLoading);
            Assert.IsFalse(_sut.IsExpanded);
            MockTanzuExplorerViewModel.VerifySet(m => m.AuthenticationRequired = true, Times.Once);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildrenAsync")]
        public async Task UpdateAllChildren_CollapsesSelf_AndLogsError_WhenAppsRequestFails()
        {
            var fakeFailedAppsResponse = new DetailedResult<List<CloudFoundryApp>> { Succeeded = false, Explanation = "junk" };

            MockCloudFoundryService.Setup(m => m
                    .GetAppsForSpaceAsync(_expectedSpace, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(fakeFailedAppsResponse);

            MockLogger.Setup(m => m
                    .Error(It.Is<string>(s => s.Contains("SpaceViewModel failed to load apps")), fakeFailedAppsResponse.Explanation))
                .Verifiable();

            Assert.IsTrue(_sut.IsExpanded);

            await _sut.UpdateAllChildrenAsync();

            Assert.IsFalse(_sut.IsLoading);
            Assert.IsFalse(_sut.IsExpanded);
            MockLogger.VerifyAll();
        }

        [TestMethod]
        [TestCategory("UpdateAllChildrenAsync")]
        public async Task UpdateAllChildren_DisplaysWarning_AndLogsError_ForAllCaughtExceptions()
        {
            var fakeException = new Exception(":(");

            MockCloudFoundryService.Setup(m => m
                    .GetAppsForSpaceAsync(_expectedSpace, _expectedSkipSslValue, _expectedRetryAmount))
                .Throws(fakeException);

            MockLogger.Setup(m => m
                    .Error(It.Is<string>(s => s.Contains("Caught exception trying to load apps in SpaceViewModel")), fakeException))
                .Verifiable();

            MockErrorDialogService.Setup(m => m
                    .DisplayWarningDialog(
                        SpaceViewModel._getAppsFailureMsg,
                        It.Is<string>(s =>
                            s.Contains("try disconnecting & logging in again"))))
                .Verifiable();

            await _sut.UpdateAllChildrenAsync();

            MockLogger.VerifyAll();
            MockErrorDialogService.VerifyAll();
        }
    }
}