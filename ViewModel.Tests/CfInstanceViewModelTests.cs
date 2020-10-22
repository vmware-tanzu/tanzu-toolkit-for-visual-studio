﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    [TestClass]
    public class CfInstanceViewModelTests : ViewModelTestSupport
    {
        private CfInstanceViewModel cfivm;

        [TestMethod]
        public void Constructor_SetsDisplayTextToInstanceName()
        {
            string instanceName = "junk";
            cfivm = new CfInstanceViewModel(new CloudFoundryInstance(instanceName, null, null), services);

            Assert.AreEqual(instanceName, cfivm.DisplayText);
        }

        [TestMethod]
        public void ChildrenAreLazilyLoaded_UponViewModelExpansion()
        {
            var fakeOrgsList = new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization("org1", "org-1-id"),
                new CloudFoundryOrganization("org2", "org-2-id"),
                new CloudFoundryOrganization("org3", "org-3-id"),
                new CloudFoundryOrganization("org4", "org-4-id")
            };

            mockCloudFoundryService.Setup(mock => mock.GetOrgsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(fakeOrgsList);

            cfivm = new CfInstanceViewModel(new CloudFoundryInstance("fake cf", null, null), services);
            
            // check presence of single placeholder child *before* CfInstanceViewModel is expanded
            Assert.AreEqual(1, cfivm.Children.Count); 
            Assert.AreEqual(null, cfivm.Children[0]);
            
            cfivm.IsExpanded = true;

            Assert.AreEqual(fakeOrgsList.Count, cfivm.Children.Count);
            mockCloudFoundryService.VerifyAll(); 
        }

        [TestMethod]
        public void LoadChildren_UpdatesAllOrgs()
        {

            var initialOrgsList = new System.Collections.ObjectModel.ObservableCollection<TreeViewItemViewModel>
            {
                new OrgViewModel(new CloudFoundryOrganization("initial org 1", "initial org 1 guid"), "address", "token", services),
                new OrgViewModel(new CloudFoundryOrganization("initial org 2", "initial org 2 guid"), "address", "token", services),
                new OrgViewModel(new CloudFoundryOrganization("initial org 3", "initial org 3 guid"), "address", "token", services)
            };

            cfivm = new CfInstanceViewModel(new CloudFoundryInstance("fake cf instance", null, null), services)
            {
                Children = initialOrgsList
            };

            var newOrgsList = new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization("initial org 1", "initial org 1 guid"),
                new CloudFoundryOrganization("initial org 2", "initial org 2 guid")
            };

            mockCloudFoundryService.Setup(mock => mock.GetOrgsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(newOrgsList);

            Assert.AreEqual(initialOrgsList.Count, cfivm.Children.Count);

            cfivm.IsExpanded = true;

            Assert.AreEqual(newOrgsList.Count, cfivm.Children.Count);
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public void LoadChildren_SetsSpecialDisplayText_WhenThereAreNoOrgs()
        {
            cfivm = new CfInstanceViewModel(new CloudFoundryInstance("fake cf instance", null, null), services);
            var emptyOrgsList = new List<CloudFoundryOrganization>();

            mockCloudFoundryService.Setup(mock => mock.GetOrgsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(emptyOrgsList);

            cfivm.IsExpanded = true;

            Assert.IsTrue(cfivm.DisplayText.Contains(" (no orgs)"));
        }
    }

}
