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
            var fakeOrg = new CloudFoundryOrganization(orgName, null);

            ovm = new OrgViewModel(fakeOrg, null, null, services);

            Assert.AreEqual(orgName, ovm.DisplayText);
        }

        [TestMethod]
        public void ChildrenAreLazilyLoaded_UponViewModelExpansion()
        {
            var fakeSpaceList = new List<CloudFoundrySpace>
            {
                new CloudFoundrySpace("spaceName1", "spaceId1"),
                new CloudFoundrySpace("spaceName2", "spaceId2"),
                new CloudFoundrySpace("spaceName3", "spaceId3"),
                new CloudFoundrySpace("spaceName4", "spaceId4")
            };

            mockCloudFoundryService.Setup(mock => mock.GetSpacesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(fakeSpaceList);

            ovm = new OrgViewModel(new CloudFoundryOrganization("fake org", null), null, null, services);

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
            mockCloudFoundryService.Verify(mock => mock.GetSpacesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(3));
        }


        [TestMethod]
        public void LoadChildren_UpdatesAllSpaces()
        {

            var initialSpacesList = new System.Collections.ObjectModel.ObservableCollection<TreeViewItemViewModel>
            {
                new SpaceViewModel(new CloudFoundrySpace("initial space 1", "initial space 1 guid"), "address", "token", services),
                new SpaceViewModel(new CloudFoundrySpace("initial space 2", "initial space 2 guid"), "address", "token", services),
                new SpaceViewModel(new CloudFoundrySpace("initial space 3", "initial space 3 guid"), "address", "token", services)
            };

            ovm = new OrgViewModel(new CloudFoundryOrganization("fake org", null), null, null, services)
            {
                Children = initialSpacesList
            };

            var newSpacesList = new List<CloudFoundrySpace>
            {
                new CloudFoundrySpace("initial space 1", "initial space 1 guid"),
                new CloudFoundrySpace("initial space 2", "initial space 2 guid")
            };

            mockCloudFoundryService.Setup(mock => mock.GetSpacesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(newSpacesList);

            Assert.AreEqual(initialSpacesList.Count, ovm.Children.Count);
            
            ovm.IsExpanded = true;

            Assert.AreEqual(newSpacesList.Count, ovm.Children.Count);
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public void LoadChildren_SetsSpecialDisplayText_WhenThereAreNoSpaces()
        {
            ovm = new OrgViewModel(new CloudFoundryOrganization("fake org", null), null, null, services);
            List<CloudFoundrySpace> emptySpacesList = new List<CloudFoundrySpace>();

            mockCloudFoundryService.Setup(mock => mock.GetSpacesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(emptySpacesList);

            ovm.IsExpanded = true;

            Assert.IsTrue(ovm.DisplayText.Contains(" (no spaces)"));
        }
    }

}
