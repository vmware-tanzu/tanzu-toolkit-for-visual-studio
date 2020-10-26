using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
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

            mockCloudFoundryService.Setup(mock => mock.GetAppsForSpaceAsync(It.IsAny<CloudFoundrySpace>()))
                .ReturnsAsync(newAppsList);

            Assert.AreEqual(initialAppsList.Count, svm.Children.Count);

            svm.IsExpanded = true;

            Assert.AreEqual(newAppsList.Count, svm.Children.Count);
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public void LoadChildren_SetsSpecialDisplayText_WhenThereAreNoApps()
        {
            svm = new SpaceViewModel(new CloudFoundrySpace("fake space", null, null), services);
            List<CloudFoundryApp> emptyAppsList = new List<CloudFoundryApp>();

            mockCloudFoundryService.Setup(mock => mock.GetAppsForSpaceAsync(It.IsAny<CloudFoundrySpace>()))
                .ReturnsAsync(emptyAppsList);

            svm.IsExpanded = true;

            Assert.IsTrue(svm.DisplayText.Contains(" (no apps)"));
        }
    }
}
