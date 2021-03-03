using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;

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
            Assert.AreEqual(OrgViewModel.loadingMsg, _sut.LoadingPlaceholder.DisplayText);
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

            _sut.Children = initialSpacesList;

            /* erase record of initial "Children" event */
            _receivedEvents.Clear();

            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(It.IsAny<CloudFoundryOrganization>(), true))
                    .ReturnsAsync(newSpacesList);

            Assert.AreEqual(initialSpacesList.Count, _sut.Children.Count);

            await _sut.LoadChildren();

            Assert.AreEqual(newSpacesList.Count, _sut.Children.Count);

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("Children", _receivedEvents[0]);
        }

        [TestMethod]
        public async Task LoadChildren_AssignsNoSpacesPlaceholder_WhenThereAreNoSpaces()
        {
            mockCloudFoundryService.Setup(mock => mock.GetSpacesForOrgAsync(It.IsAny<CloudFoundryOrganization>(), true))
                .ReturnsAsync(emptyListOfSpaces);

            await _sut.LoadChildren();

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());
            Assert.AreEqual(OrgViewModel.emptySpacesPlaceholderMsg, _sut.Children[0].DisplayText);

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("Children", _receivedEvents[0]);
        }

        [TestMethod]
        public async Task LoadChildren_SetsIsLoadingToFalse_WhenComplete()
        {
            mockCloudFoundryService.Setup(mock => mock.GetSpacesForOrgAsync(_sut.Org, true))
                .ReturnsAsync(emptyListOfSpaces);

            _sut.IsLoading = true;

            await _sut.LoadChildren();

            Assert.IsFalse(_sut.IsLoading);
        }

        [TestMethod]
        public async Task FetchChildren_ReturnsListOfSpaces_WithoutUpdatingChildren()
        {
            mockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(fakeCfOrg, true))
                    .ReturnsAsync(new List<CloudFoundrySpace>
                    {
                        new CloudFoundrySpace("fake space name 1","fake space id 1", fakeCfOrg),
                        new CloudFoundrySpace("fake space name 2","fake space id 2", fakeCfOrg)
                    });

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
    }

}
