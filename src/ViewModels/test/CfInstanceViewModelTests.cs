using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class CfInstanceViewModelTests : ViewModelTestSupport
    {
        private CfInstanceViewModel _sut;
        private List<string> _receivedEvents;

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            _sut = new CfInstanceViewModel(FakeCfInstance, Services);

            _receivedEvents = new List<string>();
            _sut.PropertyChanged += (sender, e) =>
            {
                _receivedEvents.Add(e.PropertyName);
            };

            MockUiDispatcherService.Setup(mock => mock.
                RunOnUiThread(It.IsAny<Action>()))
                    .Callback<Action>(action =>
                    {
                        // Run whatever method is passed to MockUiDispatcherService.RunOnUiThread; do not delegate to the UI Dispatcher
                        action();
                    });
        }

        [TestCleanup]
        public void TestCleanup()
        {
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public void Constructor_SetsDisplayTextToInstanceName()
        {
            Assert.AreEqual(FakeCfInstance.InstanceName, _sut.DisplayText);
        }

        [TestMethod]
        public void Constructor_SetsLoadingPlaceholder()
        {
            Assert.AreEqual(CfInstanceViewModel._loadingMsg, _sut.LoadingPlaceholder.DisplayText);
        }

        [TestMethod]
        public void Constructor_SetsEmptyPlaceholder()
        {
            Assert.AreEqual(CfInstanceViewModel._emptyOrgsPlaceholderMsg, _sut.EmptyPlaceholder.DisplayText);
        }

        [TestMethod]
        public async Task LoadChildren_UpdatesAllOrgs()
        {
            var initialOrgsList = new System.Collections.ObjectModel.ObservableCollection<TreeViewItemViewModel>
            {
                new OrgViewModel(new CloudFoundryOrganization("initial org 1", "initial org 1 guid", null), Services),
                new OrgViewModel(new CloudFoundryOrganization("initial org 2", "initial org 2 guid", null), Services),
                new OrgViewModel(new CloudFoundryOrganization("initial org 3", "initial org 3 guid", null), Services),
            };

            var newOrgsList = new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization("initial org 1", "initial org 1 guid", null),
                new CloudFoundryOrganization("initial org 2", "initial org 2 guid", null),
            };

            var fakeSuccessResult = new DetailedResult<List<CloudFoundryOrganization>>(succeeded: true, content: newOrgsList);

            _sut.Children = initialOrgsList;

            /* erase record of initial "Children" event */
            _receivedEvents.Clear();

            MockCloudFoundryService.Setup(mock => mock.
              GetOrgsForCfInstanceAsync(_sut.CloudFoundryInstance, true, It.IsAny<int>()))
                .ReturnsAsync(fakeSuccessResult);

            Assert.AreEqual(initialOrgsList.Count, _sut.Children.Count);

            await _sut.LoadChildren();

            Assert.AreEqual(newOrgsList.Count, _sut.Children.Count);

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("Children", _receivedEvents[0]);

            Assert.IsFalse(_sut.HasEmptyPlaceholder);
        }

        [TestMethod]
        public async Task LoadChildren_AssignsNoOrgsPlaceholder_WhenThereAreNoOrgs()
        {
            var fakeNoOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>(succeeded: true, content: EmptyListOfOrgs);

            MockCloudFoundryService.Setup(mock => mock.
              GetOrgsForCfInstanceAsync(_sut.CloudFoundryInstance, true, It.IsAny<int>()))
                .ReturnsAsync(fakeNoOrgsResult);

            await _sut.LoadChildren();

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());
            Assert.AreEqual(CfInstanceViewModel._emptyOrgsPlaceholderMsg, _sut.Children[0].DisplayText);

            Assert.AreEqual(_sut.EmptyPlaceholder, _sut.Children[0]);
            Assert.IsTrue(_sut.HasEmptyPlaceholder);

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("Children", _receivedEvents[0]);
        }

        [TestMethod]
        public async Task LoadChildren_SetsIsLoadingToFalse_WhenOrgsRequestSucceeds()
        {
            var fakeOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>(succeeded: true, content: EmptyListOfOrgs);

            MockCloudFoundryService.Setup(mock => mock.
              GetOrgsForCfInstanceAsync(_sut.CloudFoundryInstance, true, It.IsAny<int>()))
                .ReturnsAsync(fakeOrgsResult);

            _sut.IsLoading = true;

            await _sut.LoadChildren();

            Assert.IsFalse(_sut.IsLoading);
        }

        [TestMethod]
        public async Task LoadChildren_SetsIsLoadingToFalse_WhenOrgsRequestFails()
        {
            var fakeFailedResult = new DetailedResult<List<CloudFoundryOrganization>>(succeeded: false, content: null);

            MockCloudFoundryService.Setup(mock => mock.
              GetOrgsForCfInstanceAsync(_sut.CloudFoundryInstance, true, It.IsAny<int>()))
                .ReturnsAsync(fakeFailedResult);

            _sut.IsLoading = true;

            await _sut.LoadChildren();

            Assert.IsFalse(_sut.IsLoading);
        }

        [TestMethod]
        public async Task LoadChildren_DisplaysErrorDialog_WhenOrgsRequestFails()
        {
            var fakeFailedResult = new DetailedResult<List<CloudFoundryOrganization>>(
                succeeded: false,
                content: null,
                explanation: "junk",
                cmdDetails: FakeFailureCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
              GetOrgsForCfInstanceAsync(_sut.CloudFoundryInstance, true, It.IsAny<int>()))
                .ReturnsAsync(fakeFailedResult);

            await _sut.LoadChildren();

            Assert.IsFalse(_sut.IsLoading);

            MockErrorDialogService.Verify(mock => mock.
              DisplayErrorDialog(CfInstanceViewModel._getOrgsFailureMsg, fakeFailedResult.Explanation),
                Times.Once);
        }

        [TestMethod]
        public async Task LoadChildren_CollapsesTreeViewItem_WhenOrgsRequestFails()
        {
            var expandedViewModel = new CfInstanceViewModel(FakeCfInstance, Services)
            {
                IsExpanded = true,
            };

            expandedViewModel.PropertyChanged += (sender, e) =>
            {
                _receivedEvents.Add(e.PropertyName);
            };

            var fakeFailedResult = new DetailedResult<List<CloudFoundryOrganization>>(
                succeeded: false,
                content: null,
                explanation: "junk",
                cmdDetails: FakeFailureCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
              GetOrgsForCfInstanceAsync(expandedViewModel.CloudFoundryInstance, true, 1))
                .ReturnsAsync(fakeFailedResult);

            Assert.IsTrue(expandedViewModel.IsExpanded);

            await expandedViewModel.LoadChildren();

            Assert.IsFalse(expandedViewModel.IsLoading);
            Assert.IsFalse(expandedViewModel.IsExpanded);
            Assert.IsTrue(_receivedEvents.Contains("IsExpanded"));

            MockErrorDialogService.Verify(mock => mock.
              DisplayErrorDialog(CfInstanceViewModel._getOrgsFailureMsg, fakeFailedResult.Explanation),
                Times.Once);
        }

        [TestMethod]
        public async Task FetchChildren_ReturnsListOfOrgs_WithoutUpdatingChildren()
        {
            var fakeOrgsList = new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization("fake org name 1", "fake org id 1", FakeCfInstance),
                new CloudFoundryOrganization("fake org name 2", "fake org id 2", FakeCfInstance),
            };

            var fakeSuccessResult = new DetailedResult<List<CloudFoundryOrganization>>(succeeded: true, content: fakeOrgsList);

            MockCloudFoundryService.Setup(mock => mock.
              GetOrgsForCfInstanceAsync(_sut.CloudFoundryInstance, true, It.IsAny<int>()))
                .ReturnsAsync(fakeSuccessResult);

            /* pre-check presence of placeholder */
            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());

            var orgs = await _sut.FetchChildren();

            Assert.AreEqual(2, orgs.Count);

            /* confirm presence of placeholder */
            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());

            // property changed events should not be raised
            Assert.AreEqual(0, _receivedEvents.Count);
        }

        [TestMethod]
        public async Task FetchChildren_DisplaysErrorDialog_WhenOrgsRequestFails()
        {
            var fakeFailedResult = new DetailedResult<List<CloudFoundryOrganization>>(
                succeeded: false,
                content: null,
                explanation: "junk",
                cmdDetails: FakeFailureCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
              GetOrgsForCfInstanceAsync(_sut.CloudFoundryInstance, true, It.IsAny<int>()))
                .ReturnsAsync(fakeFailedResult);

            var result = await _sut.FetchChildren();

            CollectionAssert.AreEqual(EmptyListOfOrgs, result);

            MockErrorDialogService.Verify(mock => mock.
              DisplayErrorDialog(CfInstanceViewModel._getOrgsFailureMsg, fakeFailedResult.Explanation),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("RefreshChildren")]
        public async Task RefreshCfInstance_UpdatesChildrenOnCfInstanceViewModel()
        {
            var fakeOrgName1 = "fake org 1";
            var fakeOrgName2 = "fake org 2";
            var fakeOrgName3 = "fake org 3";
            var fakeOrgName4 = "fake org 4";

            var fakeOrgGuid1 = "fake org 1";
            var fakeOrgGuid2 = "fake org 2";
            var fakeOrgGuid3 = "fake org 3";
            var fakeOrgGuid4 = "fake org 4";

            _sut.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                // to be removed:
                new OrgViewModel(new CloudFoundryOrganization(fakeOrgName1, fakeOrgGuid1, _sut.CloudFoundryInstance), Services), 
                // to stay:
                new OrgViewModel(new CloudFoundryOrganization(fakeOrgName2, fakeOrgGuid2, _sut.CloudFoundryInstance), Services, expanded: true), // should stay expanded after refresh 
                new OrgViewModel(new CloudFoundryOrganization(fakeOrgName3, fakeOrgGuid3, _sut.CloudFoundryInstance), Services, expanded: false), // should stay collapsed after refresh 
            };

            var fakeOrgsList = new List<CloudFoundryOrganization>
            {
                // original:
                new CloudFoundryOrganization(fakeOrgName2, fakeOrgGuid2, _sut.CloudFoundryInstance), 
                new CloudFoundryOrganization(fakeOrgName3, fakeOrgGuid3, _sut.CloudFoundryInstance),
                // new:
                new CloudFoundryOrganization(fakeOrgName4, fakeOrgGuid4, _sut.CloudFoundryInstance),
            };

            var fakeSuccessResult = new DetailedResult<List<CloudFoundryOrganization>>(succeeded: true, content: fakeOrgsList);

            Assert.AreEqual(3, _sut.Children.Count);
            OrgViewModel initialChildOrg1 = (OrgViewModel)_sut.Children[0];
            OrgViewModel initialChildOrg2 = (OrgViewModel)_sut.Children[1];
            OrgViewModel initialChildOrg3 = (OrgViewModel)_sut.Children[2];
            Assert.AreEqual(fakeOrgName1, initialChildOrg1.Org.OrgName);
            Assert.AreEqual(fakeOrgName2, initialChildOrg2.Org.OrgName);
            Assert.AreEqual(fakeOrgName3, initialChildOrg3.Org.OrgName);
            Assert.IsFalse(initialChildOrg1.IsExpanded);
            Assert.IsTrue(initialChildOrg2.IsExpanded);
            Assert.IsFalse(initialChildOrg3.IsExpanded);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(_sut.CloudFoundryInstance, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeSuccessResult);

            _receivedEvents.Clear();

            await _sut.RefreshChildren();

            Assert.AreEqual(3, _sut.Children.Count);
            OrgViewModel refreshedChildOrg1 = (OrgViewModel)_sut.Children[0];
            OrgViewModel refreshedChildOrg2 = (OrgViewModel)_sut.Children[1];
            OrgViewModel refreshedChildOrg3 = (OrgViewModel)_sut.Children[2];
            Assert.AreEqual(fakeOrgName2, refreshedChildOrg1.Org.OrgName);
            Assert.AreEqual(fakeOrgName3, refreshedChildOrg2.Org.OrgName);
            Assert.AreEqual(fakeOrgName4, refreshedChildOrg3.Org.OrgName);
            Assert.IsTrue(refreshedChildOrg1.IsExpanded); // children that aren't new shouldn't change expansion
            Assert.IsFalse(refreshedChildOrg2.IsExpanded); // children that aren't new shouldn't change expansion
            Assert.IsFalse(refreshedChildOrg2.IsExpanded); // new children should start collapsed

            // property changed events should only be raised for "IsRefreshing" (1 to set as true, 1 to set as false)
            Assert.AreEqual(2, _receivedEvents.Count);
            Assert.AreEqual("IsRefreshing", _receivedEvents[0]);

            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("RefreshChildren")]
        public async Task RefreshChildren_AddsPlaceholder_WhenAllOrgsAreRemoved()
        {
            var fakeInitialOrg = new CloudFoundryOrganization("fake org name", "fake org id", parentCf: _sut.CloudFoundryInstance);
            var ovm = new OrgViewModel(fakeInitialOrg, Services);

            var fakeEmptyOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>(
                succeeded: true,
                content: new List<CloudFoundryOrganization>(), // simulate cf having lost all orgs before refresh
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(_sut.CloudFoundryInstance, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeEmptyOrgsResult);

            _sut.Children = new ObservableCollection<TreeViewItemViewModel> { ovm }; // simulate cf initially having 1 org child

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(OrgViewModel), _sut.Children[0].GetType());

            await _sut.RefreshChildren();

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());
            Assert.AreEqual(CfInstanceViewModel._emptyOrgsPlaceholderMsg, _sut.Children[0].DisplayText);
        }

        [TestMethod]
        [TestCategory("RefreshChildren")]
        public async Task RefreshChildren_RemovesPlaceholder_WhenEmptyCfGainsChildren()
        {
            // simulate cf initially having no org children
            _sut.Children = new ObservableCollection<TreeViewItemViewModel> { _sut.EmptyPlaceholder };
            _sut.HasEmptyPlaceholder = true;

            var fakeNewOrg = new CloudFoundryOrganization("fake org name", "fake org id", _sut.CloudFoundryInstance);

            var fakeSuccessfulOrgsResult = new DetailedResult<List<CloudFoundryOrganization>>(
                succeeded: true,
                content: new List<CloudFoundryOrganization>
                {
                    fakeNewOrg, // simulate cf having gained an org child before refresh
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetOrgsForCfInstanceAsync(_sut.CloudFoundryInstance, true, It.IsAny<int>()))
                    .ReturnsAsync(fakeSuccessfulOrgsResult);

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());
            Assert.AreEqual(CfInstanceViewModel._emptyOrgsPlaceholderMsg, _sut.Children[0].DisplayText);

            await _sut.RefreshChildren();

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(OrgViewModel), _sut.Children[0].GetType());
        }
    }
}
