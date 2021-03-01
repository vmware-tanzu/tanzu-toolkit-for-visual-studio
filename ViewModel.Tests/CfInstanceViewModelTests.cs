using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;

namespace Tanzu.Toolkit.VisualStudio.ViewModels.Tests
{
    [TestClass]
    public class CfInstanceViewModelTests : ViewModelTestSupport
    {
        private CfInstanceViewModel cfivm;

        [TestCleanup]
        public void TestCleanup()
        {
            mockCloudFoundryService.VerifyAll();
        }

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
                new CloudFoundryOrganization("org1", "org-1-id", null),
                new CloudFoundryOrganization("org2", "org-2-id", null),
                new CloudFoundryOrganization("org3", "org-3-id", null),
                new CloudFoundryOrganization("org4", "org-4-id", null)
            };

            mockCloudFoundryService.Setup(mock => mock.GetOrgsForCfInstanceAsync(It.IsAny<CloudFoundryInstance>(), true))
                .ReturnsAsync(fakeOrgsList);

            cfivm = new CfInstanceViewModel(new CloudFoundryInstance("fake cf", null, null), services);

            bool ChildrenPropertyChangedCalled = false;

            cfivm.PropertyChanged += (s, args) =>
            {
                if ("Children" == args.PropertyName) ChildrenPropertyChangedCalled = true;
            };

            // check presence of single placeholder child *before* CfInstanceViewModel is expanded
            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(null, cfivm.Children[0]);

            /* Invoke `LoadChildren` */
            cfivm.IsExpanded = true;

            Assert.AreEqual(fakeOrgsList.Count, cfivm.Children.Count);
            mockCloudFoundryService.VerifyAll();
            Assert.IsTrue(ChildrenPropertyChangedCalled);
        }

        [TestMethod]
        public void LoadChildren_UpdatesAllOrgs()
        {
            var initialOrgsList = new System.Collections.ObjectModel.ObservableCollection<TreeViewItemViewModel>
            {
                new OrgViewModel(new CloudFoundryOrganization("initial org 1", "initial org 1 guid", null), services),
                new OrgViewModel(new CloudFoundryOrganization("initial org 2", "initial org 2 guid", null), services),
                new OrgViewModel(new CloudFoundryOrganization("initial org 3", "initial org 3 guid", null), services)
            };

            cfivm = new CfInstanceViewModel(new CloudFoundryInstance("fake cf instance", null, null), services)
            {
                Children = initialOrgsList
            };

            var newOrgsList = new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization("initial org 1", "initial org 1 guid", null),
                new CloudFoundryOrganization("initial org 2", "initial org 2 guid", null)
            };

            bool ChildrenPropertyChangedCalled = false;

            cfivm.PropertyChanged += (s, args) =>
            {
                if ("Children" == args.PropertyName) ChildrenPropertyChangedCalled = true;
            };

            mockCloudFoundryService.Setup(mock => mock.GetOrgsForCfInstanceAsync(It.IsAny<CloudFoundryInstance>(), true))
                .ReturnsAsync(newOrgsList);

            Assert.AreEqual(initialOrgsList.Count, cfivm.Children.Count);

            /* Invoke `LoadChildren` */
            cfivm.IsExpanded = true;

            Assert.AreEqual(newOrgsList.Count, cfivm.Children.Count);
            mockCloudFoundryService.VerifyAll();
            Assert.IsTrue(ChildrenPropertyChangedCalled);
        }

        [TestMethod]
        public void LoadChildren_AssignsNoOrgsPlaceholder_WhenThereAreNoOrgs()
        {
            cfivm = new CfInstanceViewModel(new CloudFoundryInstance("fake cf instance", null, null), services);
            var emptyOrgsList = new List<CloudFoundryOrganization>();
            bool ChildrenPropertyChangedCalled = false;

            cfivm.PropertyChanged += (s, args) =>
            {
                if ("Children" == args.PropertyName) ChildrenPropertyChangedCalled = true;
            };

            mockCloudFoundryService.Setup(mock => mock.GetOrgsForCfInstanceAsync(It.IsAny<CloudFoundryInstance>(), true))
                .ReturnsAsync(emptyOrgsList);

            /* Invoke `LoadChildren` */
            cfivm.IsExpanded = true;

            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), cfivm.Children[0].GetType());
            Assert.IsTrue(ChildrenPropertyChangedCalled);
            Assert.AreEqual(CfInstanceViewModel.emptyOrgsPlaceholderMsg, cfivm.Children[0].DisplayText);
        }

        [TestMethod]
        public void LoadChildren_DisplaysLoadingPlaceholder_BeforeOrgsResultsArrive()
        {
            cfivm = new CfInstanceViewModel(new CloudFoundryInstance("fake cf instance", null, null), services);
            var emptyOrgsList = new List<CloudFoundryOrganization>();
            bool loadingMsgDisplayed = false;

            cfivm.PropertyChanged += (s, args) =>
            {
                var vm = s as CfInstanceViewModel;

                if (args.PropertyName == "Children"
                    && vm.Children.Count == 1
                    && vm.Children[0].DisplayText == CfInstanceViewModel.loadingMsg)
                {
                    loadingMsgDisplayed = true;
                }
            };

            mockCloudFoundryService.Setup(mock => mock.GetOrgsForCfInstanceAsync(It.IsAny<CloudFoundryInstance>(), true))
                .ReturnsAsync(emptyOrgsList);


            /* Invoke `LoadChildren` */
            cfivm.IsExpanded = true;

            Assert.IsTrue(loadingMsgDisplayed);
        }

        [TestMethod]
        public async Task FetchChildren_ReturnsListOfOrgs_WithoutUpdatingChildren()
        {
            var receivedEvents = new List<string>();
            var fakeCfInstance = new CloudFoundryInstance("junk", null, null);
            cfivm = new CfInstanceViewModel(fakeCfInstance, services);

            cfivm.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            mockCloudFoundryService.Setup(mock => mock.GetOrgsForCfInstanceAsync(fakeCfInstance, true))
                .ReturnsAsync(new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization("fake org name 1","fake org id 1", fakeCfInstance),
                new CloudFoundryOrganization("fake org name 2","fake org id 2", fakeCfInstance)
            });

            var orgs = await cfivm.FetchChildren();

            Assert.AreEqual(2, orgs.Count);

            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.IsNull(cfivm.Children[0]);

            // property changed events should not be raised
            Assert.AreEqual(0, receivedEvents.Count);

            mockCloudFoundryService.VerifyAll();
        }
    }

}
