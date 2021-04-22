using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;
using Tanzu.Toolkit.VisualStudio.Services;

namespace Tanzu.Toolkit.VisualStudio.ViewModels.Tests
{
    [TestClass]
    public class OrgViewModelTests : ViewModelTestSupport
    {
        private OrgViewModel _sut;
        private List<string> _receivedEvents;

        [TestInitialize]
        public void TestInit()
        {
            _sut = new OrgViewModel(fakeCfOrg, services);

            _receivedEvents = new List<string>();
            _sut.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                _receivedEvents.Add(e.PropertyName);
            };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public void Constructor_SetsDisplayTextToOrgName()
        {
            Assert.AreEqual(fakeCfOrg.OrgName, _sut.DisplayText);
        }

        [TestMethod]
        public void Constructor_SetsLoadingPlaceholder()
        {
            Assert.AreEqual(OrgViewModel._loadingMsg, _sut.LoadingPlaceholder.DisplayText);
        }

        [TestMethod]
        public void Constructor_SetsEmptyPlaceholder()
        {
            Assert.AreEqual(OrgViewModel._emptySpacesPlaceholderMsg, _sut.EmptyPlaceholder.DisplayText);
        }

        [TestMethod]
        public async Task LoadChildren_UpdatesAllSpaces()
        {
            var initialSpacesList = new System.Collections.ObjectModel.ObservableCollection<TreeViewItemViewModel>
            {
                new SpaceViewModel(new CloudFoundrySpace("initial space 1", "initial space 1 guid", null), services),
                new SpaceViewModel(new CloudFoundrySpace("initial space 2", "initial space 2 guid", null), services),
                new SpaceViewModel(new CloudFoundrySpace("initial space 3", "initial space 3 guid", null), services)
            };

            var newSpacesList = new List<CloudFoundrySpace>
            {
                new CloudFoundrySpace("initial space 1", "initial space 1 guid", null),
                new CloudFoundrySpace("initial space 2", "initial space 2 guid", null)
            };
            var fakeSucccessResponse = new DetailedResult<List<CloudFoundrySpace>>
            (
                content: newSpacesList,
                succeeded: true,
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            _sut.Children = initialSpacesList;

            /* erase record of initial "Children" event */
            _receivedEvents.Clear();

            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(It.IsAny<CloudFoundryOrganization>(), true))
                    .ReturnsAsync(fakeSucccessResponse);

            Assert.AreEqual(initialSpacesList.Count, _sut.Children.Count);

            await _sut.LoadChildren();

            Assert.AreEqual(newSpacesList.Count, _sut.Children.Count);

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("Children", _receivedEvents[0]);
            
            Assert.IsFalse(_sut.HasEmptyPlaceholder);
        }

        [TestMethod]
        public async Task LoadChildren_AssignsNoSpacesPlaceholder_WhenThereAreNoSpaces()
        {
            var fakeNoSpacesResponse = new DetailedResult<List<CloudFoundrySpace>>
            (
                content: emptyListOfSpaces,
                succeeded: true,
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(It.IsAny<CloudFoundryOrganization>(), true))
                    .ReturnsAsync(fakeNoSpacesResponse);

            await _sut.LoadChildren();

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());
            Assert.AreEqual(OrgViewModel._emptySpacesPlaceholderMsg, _sut.Children[0].DisplayText);

            Assert.AreEqual(_sut.EmptyPlaceholder, _sut.Children[0]);
            Assert.IsTrue(_sut.HasEmptyPlaceholder);

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("Children", _receivedEvents[0]);
        }

        [TestMethod]
        public async Task LoadChildren_SetsIsLoadingToFalse_WhenComplete()
        {
            var fakeNoSpacesResponse = new DetailedResult<List<CloudFoundrySpace>>
            (
                content: emptyListOfSpaces,
                succeeded: true,
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(_sut.Org, true))
                    .ReturnsAsync(fakeNoSpacesResponse);

            _sut.IsLoading = true;

            await _sut.LoadChildren();

            Assert.IsFalse(_sut.IsLoading);
        }

        [TestMethod]
        public async Task LoadChildren_DisplaysErrorDialog_WhenSpacesRequestFails()
        {
            var fakeFailedResult = new DetailedResult<List<CloudFoundrySpace>>(
                succeeded: false,
                content: null,
                explanation: "junk",
                cmdDetails: fakeFailureCmdResult);

            mockCloudFoundryService.Setup(mock => mock.
              GetSpacesForOrgAsync(_sut.Org, true))
                .ReturnsAsync(fakeFailedResult);

            await _sut.LoadChildren();

            Assert.IsFalse(_sut.IsLoading);

            mockDialogService.Verify(mock => mock.
              DisplayErrorDialog(OrgViewModel._getSpacesFailureMsg, fakeFailedResult.Explanation),
                Times.Once);
        }

        [TestMethod]
        public async Task FetchChildren_ReturnsListOfSpaces_WithoutUpdatingChildren()
        {
            List<CloudFoundrySpace> fakeSpacesList = new List<CloudFoundrySpace>
            {
                new CloudFoundrySpace("fake space name 1","fake space id 1", fakeCfOrg),
                new CloudFoundrySpace("fake space name 2","fake space id 2", fakeCfOrg)
            };

            var fakeSuccessResponse = new DetailedResult<List<CloudFoundrySpace>>
            (
                content: fakeSpacesList,
                succeeded: true,
                explanation: null,
                cmdDetails: fakeSuccessCmdResult
            );

            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeCfOrg, true))
                    .ReturnsAsync(fakeSuccessResponse);

            /* pre-check presence of placeholder */
            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());

            var spaces = await _sut.FetchChildren();

            Assert.AreEqual(2, spaces.Count);

            /* confirm presence of placeholder */
            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());

            // property changed events should not be raised
            Assert.AreEqual(0, _receivedEvents.Count);
        }

        [TestMethod]
        public async Task FetchChildren_DisplaysErrorDialog_WhenSpacesRequestFails()
        {
            var fakeFailedResult = new DetailedResult<List<CloudFoundrySpace>>(
                succeeded: false,
                content: null,
                explanation: "junk",
                cmdDetails: fakeFailureCmdResult);

            mockCloudFoundryService.Setup(mock => mock.
              GetSpacesForOrgAsync(_sut.Org, true))
                .ReturnsAsync(fakeFailedResult);

            var result = await _sut.FetchChildren();

            CollectionAssert.AreEqual(emptyListOfSpaces, result);

            mockDialogService.Verify(mock => mock.
              DisplayErrorDialog(OrgViewModel._getSpacesFailureMsg, fakeFailedResult.Explanation),
                Times.Once);
        }

    }

}
