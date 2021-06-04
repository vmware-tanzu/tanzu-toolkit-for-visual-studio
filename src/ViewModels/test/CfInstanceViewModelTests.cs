using System.Collections.Generic;
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
              GetOrgsForCfInstanceAsync(_sut.CloudFoundryInstance, true))
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
              GetOrgsForCfInstanceAsync(_sut.CloudFoundryInstance, true))
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
              GetOrgsForCfInstanceAsync(_sut.CloudFoundryInstance, true))
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
              GetOrgsForCfInstanceAsync(_sut.CloudFoundryInstance, true))
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
              GetOrgsForCfInstanceAsync(_sut.CloudFoundryInstance, true))
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
              GetOrgsForCfInstanceAsync(expandedViewModel.CloudFoundryInstance, true))
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
              GetOrgsForCfInstanceAsync(_sut.CloudFoundryInstance, true))
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
              GetOrgsForCfInstanceAsync(_sut.CloudFoundryInstance, true))
                .ReturnsAsync(fakeFailedResult);

            var result = await _sut.FetchChildren();

            CollectionAssert.AreEqual(EmptyListOfOrgs, result);

            MockErrorDialogService.Verify(mock => mock.
              DisplayErrorDialog(CfInstanceViewModel._getOrgsFailureMsg, fakeFailedResult.Explanation),
                Times.Once);
        }
    }
}
