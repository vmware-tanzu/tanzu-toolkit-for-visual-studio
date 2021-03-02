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
        private CfInstanceViewModel _sut;
        private List<string> _receivedEvents;

        [TestInitialize]
        public void TestInit()
        {
            _sut = new CfInstanceViewModel(fakeCfInstance, services);

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
        public void Constructor_SetsDisplayTextToInstanceName()
        {
            Assert.AreEqual(fakeCfInstance.InstanceName, _sut.DisplayText);
        }

        [TestMethod]
        public void Constructor_SetsLoadingPlaceholder()
        {
            Assert.AreEqual(CfInstanceViewModel.loadingMsg, _sut.LoadingPlaceholder.DisplayText);
        }

        [TestMethod]
        public async Task LoadChildren_UpdatesAllOrgs()
        {
            var initialOrgsList = new System.Collections.ObjectModel.ObservableCollection<TreeViewItemViewModel>
            {
                new OrgViewModel(new CloudFoundryOrganization("initial org 1", "initial org 1 guid", null), services),
                new OrgViewModel(new CloudFoundryOrganization("initial org 2", "initial org 2 guid", null), services),
                new OrgViewModel(new CloudFoundryOrganization("initial org 3", "initial org 3 guid", null), services)
            };

            var newOrgsList = new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization("initial org 1", "initial org 1 guid", null),
                new CloudFoundryOrganization("initial org 2", "initial org 2 guid", null)
            };

            _sut.Children = initialOrgsList;

            /* erase record of initial "Children" event */
            _receivedEvents.Clear();

            mockCloudFoundryService.Setup(mock => mock.GetOrgsForCfInstanceAsync(It.IsAny<CloudFoundryInstance>(), true))
                .ReturnsAsync(newOrgsList);

            Assert.AreEqual(initialOrgsList.Count, _sut.Children.Count);

            await _sut.LoadChildren();

            Assert.AreEqual(newOrgsList.Count, _sut.Children.Count);

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("Children", _receivedEvents[0]);
        }

        [TestMethod]
        public async Task LoadChildren_AssignsNoOrgsPlaceholder_WhenThereAreNoOrgs()
        {
            mockCloudFoundryService.Setup(mock => mock.GetOrgsForCfInstanceAsync(It.IsAny<CloudFoundryInstance>(), true))
                .ReturnsAsync(emptyListOfOrgs);

            await _sut.LoadChildren();

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());
            Assert.AreEqual(CfInstanceViewModel.emptyOrgsPlaceholderMsg, _sut.Children[0].DisplayText);
            
            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("Children", _receivedEvents[0]);
        }

        [TestMethod]
        public async Task FetchChildren_ReturnsListOfOrgs_WithoutUpdatingChildren()
        {
            mockCloudFoundryService.Setup(mock => mock.GetOrgsForCfInstanceAsync(fakeCfInstance, true))
                .ReturnsAsync(new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization("fake org name 1","fake org id 1", fakeCfInstance),
                new CloudFoundryOrganization("fake org name 2","fake org id 2", fakeCfInstance)
            });

            /* pre-check presence of placeholder */
            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());

            var orgs = await _sut.FetchChildren();

            Assert.AreEqual(2, orgs.Count);

            /* confirm presence of placeholder */
            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());

            // property changed events should not be raised
            Assert.AreEqual(0, _receivedEvents.Count);
        }
    }

}
