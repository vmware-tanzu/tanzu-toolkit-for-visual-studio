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
    public class OrgViewModelTests : ViewModelTestSupport
    {
        private OrgViewModel _sut;
        private List<string> _receivedEvents;
        private CfInstanceViewModel _fakeCfInstanceViewModel;
        private CloudFoundryOrganization _expectedOrg;
        private readonly bool _expectedSkipSslValue = false;
        private readonly int _expectedRetryAmount = 1;

        private readonly DetailedResult<List<CloudFoundrySpace>> _fakeSpacesResponse = new()
        {
            Succeeded = true,
            Content =
            [
                _fakeSpaces[0],
                _fakeSpaces[1],
                _fakeSpaces[2]
            ]
        };

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            _fakeCfInstanceViewModel = new CfInstanceViewModel(_fakeCfInstance, MockTanzuExplorerViewModel.Object, Services, expanded: true);
            _receivedEvents = [];

            MockUiDispatcherService.Setup(mock => mock.RunOnUIThreadAsync(It.IsAny<Action>()))
                .Callback<Action>(action =>
                {
                    // Run whatever method is passed to MockUiDispatcherService.RunOnUiThread; do not delegate to the UI Dispatcher
                    action();
                });

            MockThreadingService.Setup(m => m.RemoveItemFromCollectionOnUIThreadAsync(It.IsAny<ObservableCollection<TreeViewItemViewModel>>(), It.IsAny<TreeViewItemViewModel>()))
                .Callback<ObservableCollection<TreeViewItemViewModel>, TreeViewItemViewModel>((collection, item) => { collection.Remove(item); });

            MockThreadingService.Setup(m => m.AddItemToCollectionOnUIThreadAsync(It.IsAny<ObservableCollection<TreeViewItemViewModel>>(), It.IsAny<TreeViewItemViewModel>()))
                .Callback<ObservableCollection<TreeViewItemViewModel>, TreeViewItemViewModel>((collection, item) => { collection.Add(item); });

            MockTanzuExplorerViewModel.SetupGet(m => m.CloudFoundryConnection).Returns(_fakeCfInstanceViewModel);

            _sut = new OrgViewModel(_fakeCfOrg, _fakeCfInstanceViewModel, MockTanzuExplorerViewModel.Object, Services, expanded: true);

            _sut.PropertyChanged += (sender, e) => { _receivedEvents.Add(e.PropertyName); };

            _expectedOrg = _sut.Org;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsDisplayTextToOrgName()
        {
            Assert.AreEqual(_fakeCfOrg.OrgName, _sut.DisplayText);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsLoadingPlaceholder()
        {
            Assert.AreEqual(OrgViewModel._loadingMsg, _sut.LoadingPlaceholder.DisplayText);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsEmptyPlaceholder()
        {
            Assert.AreEqual(OrgViewModel._emptySpacesPlaceholderMsg, _sut.EmptyPlaceholder.DisplayText);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsParent()
        {
            Assert.AreEqual(_fakeCfInstanceViewModel, _sut.Parent);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsParentTanzuExplorer()
        {
            Assert.AreEqual(MockTanzuExplorerViewModel.Object, _sut.ParentTanzuExplorer);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_RemovesStaleChildrenOnUiThread_WhenSpacesRequestSucceeds()
        {
            /** mock 4 initial children */
            _sut.Children =
            [
                new SpaceViewModel(_fakeSpaces[0], _sut, MockTanzuExplorerViewModel.Object, Services),
                new SpaceViewModel(_fakeSpaces[1], _sut, MockTanzuExplorerViewModel.Object, Services),
                new SpaceViewModel(_fakeSpaces[2], _sut, MockTanzuExplorerViewModel.Object, Services),
                new SpaceViewModel(_fakeSpaces[3], _sut, MockTanzuExplorerViewModel.Object, Services)
            ];

            /** mock retrieving all initial children except for FakeSpaces[3] */
            MockCloudFoundryService.Setup(m => m
                    .GetSpacesForOrgAsync(_expectedOrg, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(_fakeSpacesResponse);

            await _sut.UpdateAllChildren();

            Assert.AreEqual(3, _sut.Children.Count);
            Assert.IsTrue(_sut.Children.Any(child => child is SpaceViewModel space && space.Space.SpaceName == _fakeSpaces[0].SpaceName));
            Assert.IsTrue(_sut.Children.Any(child => child is SpaceViewModel space && space.Space.SpaceName == _fakeSpaces[1].SpaceName));
            Assert.IsTrue(_sut.Children.Any(child => child is SpaceViewModel space && space.Space.SpaceName == _fakeSpaces[2].SpaceName));
            Assert.IsFalse(_sut.Children.Any(child => child is SpaceViewModel space && space.Space.SpaceName == _fakeSpaces[3].SpaceName));

            MockThreadingService.Verify(m => m.RemoveItemFromCollectionOnUIThreadAsync(_sut.Children, It.Is<SpaceViewModel>((ovm) => ovm.Space == _fakeSpaces[3])),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_AddsNewChildrenOnUiThread_WhenSpacesRequestSucceeds()
        {
            /** mock 2 initial children */
            _sut.Children =
            [
                new SpaceViewModel(_fakeSpaces[0], _sut, MockTanzuExplorerViewModel.Object, Services),
                new SpaceViewModel(_fakeSpaces[1], _sut, MockTanzuExplorerViewModel.Object, Services)
            ];

            /** mock retrieving all initial children plus FakeSpaces[2] */
            MockCloudFoundryService.Setup(m => m.GetSpacesForOrgAsync(_expectedOrg, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(_fakeSpacesResponse);

            await _sut.UpdateAllChildren();

            Assert.AreEqual(3, _sut.Children.Count);
            Assert.IsTrue(_sut.Children.Any(child => child is SpaceViewModel space && space.Space.SpaceName == _fakeSpaces[0].SpaceName));
            Assert.IsTrue(_sut.Children.Any(child => child is SpaceViewModel space && space.Space.SpaceName == _fakeSpaces[1].SpaceName));
            Assert.IsTrue(_sut.Children.Any(child => child is SpaceViewModel space && space.Space.SpaceName == _fakeSpaces[2].SpaceName));

            MockThreadingService.Verify(m => m
                    .AddItemToCollectionOnUIThreadAsync(_sut.Children, It.Is<SpaceViewModel>((ovm) => ovm.Space == _fakeSpaces[2])),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_CallsUpdateOnAllChildren_WhenSpacesRequestSucceeds()
        {
            MockCloudFoundryService.Setup(m => m.GetSpacesForOrgAsync(_expectedOrg, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(_fakeSpacesResponse);

            MockThreadingService.Setup(m => m.StartBackgroundTaskAsync(It.IsAny<Func<Task>>()))
                .Verifiable();

            await _sut.UpdateAllChildren();

            foreach (var child in _sut.Children)
            {
                if (child is SpaceViewModel space)
                {
                    MockThreadingService.Verify(m => m.StartBackgroundTaskAsync(space.UpdateAllChildren), Times.Once);
                }
                else
                {
                    Assert.Fail("All children should be SpaceViewModels at this point");
                }
            }
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_AssignsEmptyPlaceholder_WhenSpacesRequestReturnsNoSpaces()
        {
            var fakeNoSpacesResponse = _fakeSpacesResponse;
            fakeNoSpacesResponse.Content.Clear();

            /** mock 2 initial children */
            var initialChildren = new ObservableCollection<TreeViewItemViewModel>
            {
                new SpaceViewModel(_fakeSpaces[0], _sut, MockTanzuExplorerViewModel.Object, Services),
                new SpaceViewModel(_fakeSpaces[1], _sut, MockTanzuExplorerViewModel.Object, Services)
            };
            _sut.Children = new ObservableCollection<TreeViewItemViewModel>(initialChildren);

            MockCloudFoundryService.Setup(m => m.GetSpacesForOrgAsync(_expectedOrg, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(fakeNoSpacesResponse);

            Assert.IsFalse(_sut.Children.Any(child => child is PlaceholderViewModel));

            await _sut.UpdateAllChildren();

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.IsTrue(_sut.Children[0].Equals(_sut.EmptyPlaceholder));
            foreach (var child in initialChildren)
            {
                MockThreadingService.Verify(m => m.RemoveItemFromCollectionOnUIThreadAsync(_sut.Children, child), Times.Once);
            }

            MockThreadingService.Verify(m => m.AddItemToCollectionOnUIThreadAsync(_sut.Children, _sut.EmptyPlaceholder), Times.Once);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_SetsIsLoadingTrueAtStart_AndSetsIsLoadingFalseAtEnd()
        {
            MockCloudFoundryService.Setup(m => m.GetSpacesForOrgAsync(_expectedOrg, _expectedSkipSslValue, _expectedRetryAmount))
                .Callback(() =>
                {
                    // ensure IsLoading was set to true by the time spaces were queried
                    Assert.IsTrue(_sut.IsLoading);
                }).ReturnsAsync(_fakeSpacesResponse);

            Assert.IsFalse(_sut.IsLoading);
            Assert.IsTrue(_sut.IsExpanded);

            await _sut.UpdateAllChildren();

            Assert.IsFalse(_sut.IsLoading);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_RemovesEmptyPlaceholder_WhenSpacesRequestReturnsSpaces()
        {
            _sut = new OrgViewModel(_fakeCfOrg, _fakeCfInstanceViewModel, MockTanzuExplorerViewModel.Object, Services, expanded: true)
            {
                Children =
                [
                    _sut.EmptyPlaceholder
                ]
            };

            var fakeFailedResult = new DetailedResult<List<CloudFoundrySpace>>(succeeded: false, content: null, explanation: "junk", cmdDetails: _fakeFailureCmdResult)
            {
                FailureType = FailureType.InvalidRefreshToken
            };

            /** mock 3 new children */
            MockCloudFoundryService.Setup(m => m.GetSpacesForOrgAsync(_expectedOrg, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(_fakeSpacesResponse);

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());
            Assert.AreEqual(_sut.EmptyPlaceholder.GetType(), _sut.Children[0].GetType());
            Assert.AreEqual(_sut.EmptyPlaceholder.DisplayText, _sut.Children[0].DisplayText);

            await _sut.UpdateAllChildren();

            Assert.AreEqual(3, _sut.Children.Count);
            Assert.IsFalse(_sut.Children.Any(child => child is PlaceholderViewModel));
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_RemovesLoadingPlaceholder_WhenSpacesRequestReturnsSpaces()
        {
            var fakeNoSpacesResponse = _fakeSpacesResponse;
            fakeNoSpacesResponse.Content.Clear();

            // mock 2 initial children
            _sut.Children =
            [
                new SpaceViewModel(_fakeSpaces[0], _sut, MockTanzuExplorerViewModel.Object, Services),
                new SpaceViewModel(_fakeSpaces[1], _sut, MockTanzuExplorerViewModel.Object, Services)
            ];

            MockCloudFoundryService.Setup(m => m.GetSpacesForOrgAsync(_expectedOrg, _expectedSkipSslValue, _expectedRetryAmount))
                .Callback(() =>
                {
                    // ensure loading placeholder present at time of loading fresh children
                    Assert.IsTrue(_sut.IsLoading);
                    Assert.IsTrue(_sut.Children.Contains(_sut.LoadingPlaceholder));
                }).ReturnsAsync(fakeNoSpacesResponse);

            await _sut.UpdateAllChildren();

            Assert.IsFalse(_sut.Children.Contains(_sut.LoadingPlaceholder));
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_CollapsesSelf_AndSetsAuthRequiredTrue_WhenSpacesRequestFailsWithInvalidRefreshToken()
        {
            var fakeInvalidTokenResponse = new DetailedResult<List<CloudFoundrySpace>> { Succeeded = false, Explanation = "junk", FailureType = FailureType.InvalidRefreshToken };

            MockCloudFoundryService.Setup(m => m.GetSpacesForOrgAsync(_expectedOrg, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(fakeInvalidTokenResponse);
            MockTanzuExplorerViewModel.SetupSet(m => m.AuthenticationRequired = true).Verifiable();

            Assert.IsTrue(_sut.IsExpanded);
            Assert.IsFalse(_sut.ParentTanzuExplorer.AuthenticationRequired);

            await _sut.UpdateAllChildren();

            Assert.IsFalse(_sut.IsLoading);
            Assert.IsFalse(_sut.IsExpanded);
            MockTanzuExplorerViewModel.VerifyAll();
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_CollapsesSelf_AndLogsError_WhenSpacesRequestFails()
        {
            var fakeFailedSpacesResponse = new DetailedResult<List<CloudFoundrySpace>> { Succeeded = false, Explanation = "junk" };

            MockCloudFoundryService.Setup(m => m.GetSpacesForOrgAsync(_expectedOrg, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(fakeFailedSpacesResponse);

            MockLogger.Setup(m => m.Error(It.Is<string>(s => s.Contains("OrgViewModel failed to load spaces")), fakeFailedSpacesResponse.Explanation))
                .Verifiable();

            Assert.IsTrue(_sut.IsExpanded);

            await _sut.UpdateAllChildren();

            Assert.IsFalse(_sut.IsLoading);
            Assert.IsFalse(_sut.IsExpanded);
            MockLogger.VerifyAll();
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_DisplaysWarning_AndLogsError_ForAllCaughtExceptions()
        {
            var fakeException = new Exception(":(");

            MockCloudFoundryService
                .Setup(m => m.GetSpacesForOrgAsync(_expectedOrg, _expectedSkipSslValue, _expectedRetryAmount))
                .Throws(fakeException);

            MockLogger
                .Setup(m => m.Error(It.Is<string>(s => s.Contains("Caught exception trying to load spaces in OrgViewModel")), fakeException))
                .Verifiable();

            MockErrorDialogService
                .Setup(m => m.DisplayWarningDialog(OrgViewModel._getSpacesFailureMsg, It.Is<string>(s => s.Contains("try disconnecting & logging in again"))))
                .Verifiable();

            await _sut.UpdateAllChildren();

            MockLogger.VerifyAll();
            MockErrorDialogService.VerifyAll();
        }
    }
}