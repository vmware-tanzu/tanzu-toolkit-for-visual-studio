using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Models;

namespace Tanzu.Toolkit.VisualStudio.ViewModels.Tests
{
    [TestClass]
    public class SpaceViewModelTests : ViewModelTestSupport
    {
        private SpaceViewModel svm;

        [TestMethod]
        public void Constructor_SetsDisplayTextToSpaceName()
        {
            string spaceName = "junk";
            string spaceId = "junk";
            var fakeSpace = new CloudFoundrySpace(spaceName, spaceId, null);

            svm = new SpaceViewModel(fakeSpace, services);

            Assert.AreEqual(spaceName, svm.DisplayText);
        }

        [TestMethod]
        public void LoadChildren_UpdatesAllSpaces()
        {
            var initialAppsList = new System.Collections.ObjectModel.ObservableCollection<TreeViewItemViewModel>
            {
                new AppViewModel(new CloudFoundryApp("initial app 1", null, null), services),
                new AppViewModel(new CloudFoundryApp("initial app 2", null, null), services),
                new AppViewModel(new CloudFoundryApp("initial app 3", null, null), services),
            };

            svm = new SpaceViewModel(new CloudFoundrySpace("fake space", null, null), services)
            {
                Children = initialAppsList
            };

            var newAppsList = new List<CloudFoundryApp>
            {
                new CloudFoundryApp("initial app 1", null, null),
                new CloudFoundryApp("initial app 2", null, null)
            };

            mockCloudFoundryService.Setup(mock => mock.GetAppsForSpaceAsync(It.IsAny<CloudFoundrySpace>(), true))
                .ReturnsAsync(newAppsList);

            Assert.AreEqual(initialAppsList.Count, svm.Children.Count);

            svm.IsExpanded = true;

            Assert.AreEqual(newAppsList.Count, svm.Children.Count);
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public void LoadChildren_AssignsNoAppsPlaceholder_WhenThereAreNoApps()
        {
            svm = new SpaceViewModel(new CloudFoundrySpace("fake cf space", null, null), services);
            var emptyAppsList = new List<CloudFoundryApp>();
            bool ChildrenPropertyChangedCalled = false;

            svm.PropertyChanged += (s, args) =>
            {
                if ("Children" == args.PropertyName) ChildrenPropertyChangedCalled = true;
            };

            mockCloudFoundryService.Setup(mock => mock.GetAppsForSpaceAsync(It.IsAny<CloudFoundrySpace>(), true))
                .ReturnsAsync(emptyAppsList);

            /* Invoke `LoadChildren` */
            svm.IsExpanded = true;

            Assert.AreEqual(1, svm.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), svm.Children[0].GetType());
            Assert.IsTrue(ChildrenPropertyChangedCalled);
            Assert.AreEqual(SpaceViewModel.emptyAppsPlaceholderMsg, svm.Children[0].DisplayText);
        }

        [TestMethod]
        public void LoadChildren_DisplaysLoadingPlaceholder_BeforeAppsResultsArrive()
        {
            svm = new SpaceViewModel(new CloudFoundrySpace("fake cf space", null, null), services);
            var emptyAppsList = new List<CloudFoundryApp>();
            bool loadingMsgDisplayed = false;

            svm.PropertyChanged += (s, args) =>
            {
                var vm = s as SpaceViewModel;

                if (args.PropertyName == "Children"
                    && vm.Children.Count == 1
                    && vm.Children[0].DisplayText == SpaceViewModel.loadingMsg)
                {
                    loadingMsgDisplayed = true;
                }
            };

            mockCloudFoundryService.Setup(mock => mock.GetAppsForSpaceAsync(It.IsAny<CloudFoundrySpace>(), true))
                .ReturnsAsync(emptyAppsList);


            /* Invoke `LoadChildren` */
            svm.IsExpanded = true;

            Assert.IsTrue(loadingMsgDisplayed);
        }

        [TestMethod]
        public async Task FetchChildren_ReturnsListOfApps_WithoutUpdatingChildren()
        {
            var receivedEvents = new List<string>();
            var fakeSpace = new CloudFoundrySpace("junk", null, null);
            svm = new SpaceViewModel(fakeSpace, services);

            svm.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            mockCloudFoundryService.Setup(mock => mock.GetAppsForSpaceAsync(fakeSpace, true))
                .ReturnsAsync(new List<CloudFoundryApp>
            {
                new CloudFoundryApp("fake app name 1","fake app id 1", fakeSpace),
                new CloudFoundryApp("fake app name 2","fake app id 2", fakeSpace)
            });

            var apps = await svm.FetchChildren();

            Assert.AreEqual(2, apps.Count);

            Assert.AreEqual(1, svm.Children.Count);
            Assert.IsNull(svm.Children[0]);

            // property changed events should not be raised
            Assert.AreEqual(0, receivedEvents.Count);

            mockCloudFoundryService.VerifyAll();
        }
    }
}
