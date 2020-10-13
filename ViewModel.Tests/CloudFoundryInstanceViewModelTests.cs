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
            var fakeOrgsList = new List<string>() { "fake Org 1", "fake Org 2", "fake Org 3" };
            mockCloudFoundryService.Setup(mock => mock.GetOrgNamesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(fakeOrgsList);

            cfivm = new CloudFoundryInstanceViewModel(new CloudFoundryInstance("fake cf", null, null), services);
            
            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(null, cfivm.Children[0]);
            cfivm.IsExpanded = true;

            Assert.AreEqual(fakeOrgsList.Count, cfivm.Children.Count);
            mockCloudFoundryService.VerifyAll(); // TODO: call CfService in the real implementation
        } 
    }

}
