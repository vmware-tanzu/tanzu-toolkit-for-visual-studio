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
    public class CfInstanceViewModelTests : ViewModelTestSupport
    {
        private CfInstanceViewModel _sut;
        private List<string> _receivedEvents;
        private TasExplorerViewModel _fakeTasExplorerViewModel;
        private CloudFoundryInstance _expectedCf;
        private readonly bool _expectedSkipSslValue = false;
        private readonly int _expectedRetryAmount = 1;
        private readonly DetailedResult<List<CloudFoundryOrganization>> _fakeOrgsResponse = new DetailedResult<List<CloudFoundryOrganization>>
        {
            Succeeded = true,
            Content = new List<CloudFoundryOrganization>
            {
                _fakeOrgs[0],
                _fakeOrgs[1],
                _fakeOrgs[2],
            }
        };

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            MockUiDispatcherService.Setup(mock => mock.
                RunOnUiThreadAsync(It.IsAny<Action>()))
                    .Callback<Action>(action =>
                    {
                        // Run whatever method is passed to MockUiDispatcherService.RunOnUiThread; do not delegate to the UI Dispatcher
                        action();
                    });

            MockThreadingService.Setup(m => m
              .RemoveItemFromCollectionOnUiThreadAsync(It.IsAny<ObservableCollection<TreeViewItemViewModel>>(), It.IsAny<TreeViewItemViewModel>()))
                .Callback<ObservableCollection<TreeViewItemViewModel>, TreeViewItemViewModel>((collection, item) =>
                {
                    collection.Remove(item);
                });

            MockThreadingService.Setup(m => m
              .AddItemToCollectionOnUiThreadAsync(It.IsAny<ObservableCollection<TreeViewItemViewModel>>(), It.IsAny<TreeViewItemViewModel>()))
                .Callback<ObservableCollection<TreeViewItemViewModel>, TreeViewItemViewModel>((collection, item) =>
                {
                    collection.Add(item);
                });

            _fakeTasExplorerViewModel = new TasExplorerViewModel(Services);
            _sut = new CfInstanceViewModel(_fakeCfInstance, _fakeTasExplorerViewModel, Services);
            _sut = new CfInstanceViewModel(_fakeCfInstance, _fakeTasExplorerViewModel, Services, expanded: true);

            _receivedEvents = new List<string>();
            _sut.PropertyChanged += (sender, e) =>
            {
                _receivedEvents.Add(e.PropertyName);
            };

            _expectedCf = _sut.CloudFoundryInstance;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsDisplayTextToInstanceName()
        {
            Assert.AreEqual(_fakeCfInstance.InstanceName, _sut.DisplayText);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsLoadingPlaceholder()
        {
            Assert.AreEqual(CfInstanceViewModel._loadingMsg, _sut.LoadingPlaceholder.DisplayText);
        }

        [TestMethod]
        [TestCategory("ctor")]  
        public void Constructor_SetsEmptyPlaceholder()
        {
            Assert.AreEqual(CfInstanceViewModel._emptyOrgsPlaceholderMsg, _sut.EmptyPlaceholder.DisplayText);
        }

        [TestMethod]
        [TestCategory("ctor")]
        public void Constructor_SetsParentTasExplorer()
        {
            Assert.AreEqual(_fakeTasExplorerViewModel, _sut.ParentTasExplorer);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_RemovesStaleChildrenOnUiThread_WhenOrgsRequestSucceeds()
        {
            /** mock 4 initial children */
            _sut.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new OrgViewModel(_fakeOrgs[0], _sut, _fakeTasExplorerViewModel, Services),
                new OrgViewModel(_fakeOrgs[1], _sut, _fakeTasExplorerViewModel, Services),
                new OrgViewModel(_fakeOrgs[2], _sut, _fakeTasExplorerViewModel, Services),
                new OrgViewModel(_fakeOrgs[3], _sut, _fakeTasExplorerViewModel, Services),
            };

            /** mock retrieving all initial children except for FakeOrgs[3] */
            MockCloudFoundryService.Setup(m => m
              .GetOrgsForCfInstanceAsync(_expectedCf, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(_fakeOrgsResponse);

            await _sut.UpdateAllChildren();

            Assert.AreEqual(3, _sut.Children.Count);
            Assert.IsTrue(_sut.Children.Any(child => child is OrgViewModel org && org.Org.OrgName == _fakeOrgs[0].OrgName));
            Assert.IsTrue(_sut.Children.Any(child => child is OrgViewModel org && org.Org.OrgName == _fakeOrgs[1].OrgName));
            Assert.IsTrue(_sut.Children.Any(child => child is OrgViewModel org && org.Org.OrgName == _fakeOrgs[2].OrgName));
            Assert.IsFalse(_sut.Children.Any(child => child is OrgViewModel org && org.Org.OrgName == _fakeOrgs[3].OrgName));

            MockThreadingService.Verify(m => m
              .RemoveItemFromCollectionOnUiThreadAsync(_sut.Children, It.Is<OrgViewModel>((ovm) => ovm.Org == _fakeOrgs[3])),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_AddsNewChildrenOnUiThread_WhenOrgsRequestSucceeds()
        {
            /** mock 2 initial children */
            _sut.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new OrgViewModel(_fakeOrgs[0], _sut, _fakeTasExplorerViewModel, Services),
                new OrgViewModel(_fakeOrgs[1], _sut, _fakeTasExplorerViewModel, Services),
            };

            /** mock retrieving all initial children plus FakeOrgs[2] */
            MockCloudFoundryService.Setup(m => m
              .GetOrgsForCfInstanceAsync(_expectedCf, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(_fakeOrgsResponse);

            await _sut.UpdateAllChildren();

            Assert.AreEqual(3, _sut.Children.Count);
            Assert.IsTrue(_sut.Children.Any(child => child is OrgViewModel org && org.Org.OrgName == _fakeOrgs[0].OrgName));
            Assert.IsTrue(_sut.Children.Any(child => child is OrgViewModel org && org.Org.OrgName == _fakeOrgs[1].OrgName));
            Assert.IsTrue(_sut.Children.Any(child => child is OrgViewModel org && org.Org.OrgName == _fakeOrgs[2].OrgName));

            MockThreadingService.Verify(m => m
              .AddItemToCollectionOnUiThreadAsync(_sut.Children, It.Is<OrgViewModel>((ovm) => ovm.Org == _fakeOrgs[2])),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_CallsUpdateOnAllChildren_WhenOrgsRequestSucceeds()
        {
            MockCloudFoundryService.Setup(m => m
              .GetOrgsForCfInstanceAsync(_expectedCf, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(_fakeOrgsResponse);

            MockThreadingService.Setup(m => m
              .StartBackgroundTask(It.IsAny<Func<Task>>()))
                .Verifiable();

            await _sut.UpdateAllChildren();

            foreach (var child in _sut.Children)
            {
                if (child is OrgViewModel org)
                {
                    MockThreadingService.Verify(m => m
                      .StartBackgroundTask(org.UpdateAllChildren), Times.Once);
                }
                else
                {
                    Assert.Fail("All children should be OrgViewModels at this point");
                }
            }
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_AssignsEmptyPlaceholder_WhenOrgsRequestReturnsNoOrgs()
        {
            var fakeNoOrgsResponse = _fakeOrgsResponse;
            fakeNoOrgsResponse.Content.Clear();

            /** mock 2 initial children */
            var initialChildren = new ObservableCollection<TreeViewItemViewModel>
            {
                new OrgViewModel(_fakeOrgs[0], _sut, _fakeTasExplorerViewModel, Services),
                new OrgViewModel(_fakeOrgs[1], _sut, _fakeTasExplorerViewModel, Services),
            };
            _sut.Children = new ObservableCollection<TreeViewItemViewModel>(initialChildren);

            MockCloudFoundryService.Setup(m => m
              .GetOrgsForCfInstanceAsync(_expectedCf, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(fakeNoOrgsResponse);

            Assert.IsFalse(_sut.Children.Any(child => child is PlaceholderViewModel));

            await _sut.UpdateAllChildren();

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.IsTrue(_sut.Children[0].Equals(_sut.EmptyPlaceholder));
            foreach (var child in initialChildren)
            {
                MockThreadingService.Verify(m => m.RemoveItemFromCollectionOnUiThreadAsync(_sut.Children, child), Times.Once);
            }
            MockThreadingService.Verify(m => m.AddItemToCollectionOnUiThreadAsync(_sut.Children, _sut.EmptyPlaceholder), Times.Once);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_SetsIsLoadingTrueAtStart_AndSetsIsLoadingFalseAtEnd()
        {
            MockCloudFoundryService.Setup(m => m.GetOrgsForCfInstanceAsync(_expectedCf, _expectedSkipSslValue, _expectedRetryAmount))
                .Callback(() =>
                {
                    // ensure IsLoading was set to true by the time orgs were queried
                    Assert.IsTrue(_sut.IsLoading);
                }).ReturnsAsync(_fakeOrgsResponse);

            Assert.IsFalse(_sut.IsLoading);
            Assert.IsTrue(_sut.IsExpanded);

            await _sut.UpdateAllChildren();

            Assert.IsFalse(_sut.IsLoading);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_RemovesEmptyPlaceholder_WhenOrgsRequestReturnsOrgs()
        {
            _sut.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                _sut.EmptyPlaceholder,
            };

            /** mock 3 new children */
            MockCloudFoundryService.Setup(m => m
              .GetOrgsForCfInstanceAsync(_expectedCf, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(_fakeOrgsResponse);

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.IsTrue(_sut.Children[0].Equals(_sut.EmptyPlaceholder));

            await _sut.UpdateAllChildren();

            Assert.AreEqual(3, _sut.Children.Count);
            Assert.IsFalse(_sut.Children.Any(child => child is PlaceholderViewModel));
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_RemovesLoadingPlaceholder_WhenOrgsRequestReturnsOrgs()
        {
            var fakeNoOrgsResponse = _fakeOrgsResponse;
            fakeNoOrgsResponse.Content.Clear();

            /** mock 2 initial children */
            _sut.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new OrgViewModel(_fakeOrgs[0], _sut, _fakeTasExplorerViewModel, Services),
                new OrgViewModel(_fakeOrgs[1], _sut, _fakeTasExplorerViewModel, Services),
            };

            MockCloudFoundryService.Setup(m => m
              .GetOrgsForCfInstanceAsync(_expectedCf, _expectedSkipSslValue, _expectedRetryAmount))
                .Callback(() =>
                {
                    // ensure loading placeholder present at time of loading fresh children
                    Assert.IsTrue(_sut.IsLoading);
                    Assert.IsTrue(_sut.Children.Contains(_sut.LoadingPlaceholder));

                }).ReturnsAsync(fakeNoOrgsResponse);

            await _sut.UpdateAllChildren();

            Assert.IsFalse(_sut.Children.Contains(_sut.LoadingPlaceholder));
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_CollapsesSelf_AndSetsAuthRequiredTrue_WhenOrgsRequestFailsWithInvalidRefreshToken()
        {
            var fakeInvalidTokenResponse = new DetailedResult<List<CloudFoundryOrganization>>
            {
                Succeeded = false,
                Explanation = "junk",
                FailureType = FailureType.InvalidRefreshToken,
            };

            MockCloudFoundryService.Setup(m => m
              .GetOrgsForCfInstanceAsync(_expectedCf, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(fakeInvalidTokenResponse);

            Assert.IsTrue(_sut.IsExpanded);
            Assert.IsFalse(_sut.ParentTasExplorer.AuthenticationRequired);

            await _sut.UpdateAllChildren();

            Assert.IsFalse(_sut.IsLoading);
            Assert.IsFalse(_sut.IsExpanded);
            Assert.IsTrue(_sut.ParentTasExplorer.AuthenticationRequired);
        }

        [TestMethod]
        [TestCategory("UpdateAllChildren")]
        public async Task UpdateAllChildren_CollapsesSelf_AndLogsError_WhenOrgsRequestFails()
        {
            var fakeFailedOrgsResponse = new DetailedResult<List<CloudFoundryOrganization>>
            {
                Succeeded = false,
                Explanation = "junk",
            };

            MockCloudFoundryService.Setup(m => m
              .GetOrgsForCfInstanceAsync(_expectedCf, _expectedSkipSslValue, _expectedRetryAmount))
                .ReturnsAsync(fakeFailedOrgsResponse);

            MockLogger.Setup(m => m
              .Error(It.Is<string>(s => s.Contains("CfInstanceViewModel failed to load orgs")), fakeFailedOrgsResponse.Explanation))
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

            MockCloudFoundryService.Setup(m => m
              .GetOrgsForCfInstanceAsync(_expectedCf, _expectedSkipSslValue, _expectedRetryAmount))
                .Throws(fakeException);

            MockLogger.Setup(m => m
              .Error(It.Is<string>(s => s.Contains("Caught exception trying to load orgs in CfInstanceViewModel")), fakeException))
                .Verifiable();

            MockErrorDialogService.Setup(m => m
              .DisplayWarningDialog(
                CfInstanceViewModel._getOrgsFailureMsg,
                It.Is<string>(s => s.Contains("try disconnecting & logging in again")&& s.Contains("If this issue persists, please contact dotnetdevx@groups.vmware.com"))))
                .Verifiable();

            await _sut.UpdateAllChildren();

            MockLogger.VerifyAll();
            MockErrorDialogService.VerifyAll();
        }
    }
}
