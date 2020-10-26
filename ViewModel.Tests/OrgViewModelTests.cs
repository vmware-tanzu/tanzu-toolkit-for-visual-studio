using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    [TestClass]
    public class OrgViewModelTests : ViewModelTestSupport
    {
        private OrgViewModel ovm;

        [TestMethod]
        public void Constructor_SetsDisplayTextToOrgName()
        {
            string orgName = "junk";
            var fakeOrg = new CloudFoundryOrganization(orgName, null, null);

            ovm = new OrgViewModel(fakeOrg, services);

            Assert.AreEqual(orgName, ovm.DisplayText);
        }

        [TestMethod]
        public void ChildrenAreLazilyLoaded_UponViewModelExpansion()
        {
            var fakeSpaceList = new List<CloudFoundrySpace>
            {
                new CloudFoundrySpace("spaceName1", "spaceId1", null),
                new CloudFoundrySpace("spaceName2", "spaceId2", null),
                new CloudFoundrySpace("spaceName3", "spaceId3", null),
                new CloudFoundrySpace("spaceName4", "spaceId4", null)
            };

            mockCloudFoundryService.Setup(mock => mock.GetSpacesForOrgAsync(It.IsAny<CloudFoundryOrganization>()))
                .ReturnsAsync(fakeSpaceList);

            ovm = new OrgViewModel(new CloudFoundryOrganization("fake org", null, null), services);

            // check presence of single placeholder child *before* CfInstanceViewModel is expanded
            Assert.AreEqual(1, ovm.Children.Count);
            Assert.AreEqual(null, ovm.Children[0]);

            // expand several times to ensure children re-loaded each time
            ovm.IsExpanded = true;
            ovm.IsExpanded = false;
            ovm.IsExpanded = true;
            ovm.IsExpanded = false;
            ovm.IsExpanded = true;

            Assert.AreEqual(fakeSpaceList.Count, ovm.Children.Count);
            mockCloudFoundryService.Verify(mock =>
                mock.GetSpacesForOrgAsync(It.IsAny<CloudFoundryOrganization>()), Times.Exactly(3));
        }


        [TestMethod]
        public void LoadChildren_UpdatesAllSpaces()
        {

            var initialSpacesList = new System.Collections.ObjectModel.ObservableCollection<TreeViewItemViewModel>
            {
                new SpaceViewModel(new CloudFoundrySpace("initial space 1", "initial space 1 guid", null), services),
                new SpaceViewModel(new CloudFoundrySpace("initial space 2", "initial space 2 guid", null), services),
                new SpaceViewModel(new CloudFoundrySpace("initial space 3", "initial space 3 guid", null), services)
            };

            ovm = new OrgViewModel(new CloudFoundryOrganization("fake org", null, null), services)
            {
                Children = initialSpacesList
            };

            var newSpacesList = new List<CloudFoundrySpace>
            {
                new CloudFoundrySpace("initial space 1", "initial space 1 guid", null),
                new CloudFoundrySpace("initial space 2", "initial space 2 guid", null)
            };

            mockCloudFoundryService.Setup(mock => mock.GetSpacesForOrgAsync(It.IsAny<CloudFoundryOrganization>()))
                .ReturnsAsync(newSpacesList);

            Assert.AreEqual(initialSpacesList.Count, ovm.Children.Count);

            ovm.IsExpanded = true;

            Assert.AreEqual(newSpacesList.Count, ovm.Children.Count);
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public void LoadChildren_SetsSpecialDisplayText_WhenThereAreNoSpaces()
        {
            ovm = new OrgViewModel(new CloudFoundryOrganization("fake org", null, null), services);
            List<CloudFoundrySpace> emptySpacesList = new List<CloudFoundrySpace>();

            mockCloudFoundryService.Setup(mock => mock.GetSpacesForOrgAsync(It.IsAny<CloudFoundryOrganization>()))
                .ReturnsAsync(emptySpacesList);

            ovm.IsExpanded = true;

            Assert.IsTrue(ovm.DisplayText.Contains(" (no spaces)"));
        }
    }

}
