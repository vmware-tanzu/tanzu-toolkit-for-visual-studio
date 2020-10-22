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
            var fakeSpaceNamesList = new List<string>
            {
                "space1",
                "space2",
                "space3",
                "space4"
            };

            mockCloudFoundryService.Setup(mock => mock.GetSpaceNamesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(fakeSpaceNamesList);

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

            Assert.AreEqual(fakeSpaceNamesList.Count, ovm.Children.Count);
            mockCloudFoundryService.Verify(mock => mock.GetSpaceNamesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(3));
        }


        [TestMethod]
        public void LoadChildren_UpdatesAllSpaces()
        {

            var initialSpacesList = new System.Collections.ObjectModel.ObservableCollection<TreeViewItemViewModel>
            {
                new SpaceViewModel(new CloudFoundrySpace("initial space 1"), services),
                new SpaceViewModel(new CloudFoundrySpace("initial space 2"), services),
                new SpaceViewModel(new CloudFoundrySpace("initial space 3"), services)
            };

            ovm = new OrgViewModel(new CloudFoundryOrganization("fake org", null), null, null, services)
            {
                Children = initialSpacesList
            };

            var newSpacesList = new List<string>
            {
                "initial space 1",
                "new space"
            };

            mockCloudFoundryService.Setup(mock => mock.GetSpaceNamesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
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
            List<string> emptySpacesList = new List<string>();

            mockCloudFoundryService.Setup(mock => mock.GetSpaceNamesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(emptySpacesList);

            ovm.IsExpanded = true;

            Assert.IsTrue(ovm.DisplayText.Contains(" (no spaces)"));
        }
    }

}
