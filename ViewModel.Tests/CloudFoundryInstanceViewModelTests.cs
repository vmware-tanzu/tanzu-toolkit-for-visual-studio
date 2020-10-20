using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    [TestClass]
    public class CloudFoundryInstanceViewModelTests : ViewModelTestSupport
    {
        private CloudFoundryInstanceViewModel cfivm;

        [TestMethod]
        public void ChildrenAreLazilyLoaded_UponViewModelExpansion()
        {
            const string org1Name = "org1";
            const string org2Name = "org2";
            const string org3Name = "org3";
            const string org4Name = "org4";
            const string org1Guid = "org-1-id";
            const string org2Guid = "org-2-id";
            const string org3Guid = "org-3-id";
            const string org4Guid = "org-4-id";

            var fakeOrgsList = new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization(org1Name, org1Guid),
                new CloudFoundryOrganization(org2Name, org2Guid),
                new CloudFoundryOrganization(org3Name, org3Guid),
                new CloudFoundryOrganization(org4Name, org4Guid)
            };

            mockCloudFoundryService.Setup(mock => mock.GetOrgsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(fakeOrgsList);

            cfivm = new CloudFoundryInstanceViewModel(new CloudFoundryInstance("fake cf", null, null), services);
            
            // check presence of single placeholder child *before* CfInstanceViewModel is expanded
            Assert.AreEqual(1, cfivm.Children.Count); 
            Assert.AreEqual(null, cfivm.Children[0]);
            
            cfivm.IsExpanded = true;

            Assert.AreEqual(fakeOrgsList.Count, cfivm.Children.Count);
            mockCloudFoundryService.VerifyAll(); 
        } 
    }

}
