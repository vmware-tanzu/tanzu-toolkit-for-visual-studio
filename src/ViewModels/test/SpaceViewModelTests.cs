using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class SpaceViewModelTests : ViewModelTestSupport
    {
        private SpaceViewModel _sut;
        private List<string> _receivedEvents;

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            _sut = new SpaceViewModel(FakeCfSpace, Services);

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
        public void Constructor_SetsDisplayTextToSpaceName()
        {
            Assert.AreEqual(FakeCfSpace.SpaceName, _sut.DisplayText);
        }

        [TestMethod]
        public void Constructor_SetsLoadingPlaceholder()
        {
            Assert.AreEqual(SpaceViewModel.LoadingMsg, _sut.LoadingPlaceholder.DisplayText);
        }

        [TestMethod]
        public void Constructor_SetsEmptyPlaceholder()
        {
            Assert.AreEqual(SpaceViewModel.EmptyAppsPlaceholderMsg, _sut.EmptyPlaceholder.DisplayText);
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
                GetAppsForSpaceAsync(_sut.Space, true))
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
                GetAppsForSpaceAsync(_sut.Space, true))
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
                GetAppsForSpaceAsync(_sut.Space, true))
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
                GetAppsForSpaceAsync(_sut.Space, true))
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
              GetAppsForSpaceAsync(_sut.Space, true))
                .ReturnsAsync(fakeFailedResult);

            await _sut.LoadChildren();

            Assert.IsFalse(_sut.IsLoading);

            MockDialogService.Verify(mock => mock.
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
              GetAppsForSpaceAsync(_sut.Space, true))
                .ReturnsAsync(fakeFailedResult);

            var result = await _sut.FetchChildren();

            CollectionAssert.AreEqual(EmptyListOfApps, result);

            MockDialogService.Verify(mock => mock.
              DisplayErrorDialog(SpaceViewModel._getAppsFailureMsg, fakeFailedResult.Explanation),
                Times.Once);
        }
    }
}
