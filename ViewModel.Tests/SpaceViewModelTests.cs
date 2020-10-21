using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            var fakeSpace = new CloudFoundrySpace(spaceName);

            svm = new SpaceViewModel(fakeSpace, services);

            Assert.AreEqual(spaceName, svm.DisplayText);
        }

    }
}
