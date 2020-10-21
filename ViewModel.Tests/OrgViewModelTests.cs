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

            ovm.IsExpanded = true;

            Assert.AreEqual(fakeSpaceNamesList.Count, ovm.Children.Count);
            mockCloudFoundryService.VerifyAll();
        }
    }

}
