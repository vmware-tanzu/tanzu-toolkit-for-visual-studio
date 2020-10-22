using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace TanzuForVS.ViewModels
{
    [TestClass]
    public class TreeViewItemViewModelTests : ViewModelTestSupport
    {
        TestTreeViewItemViewModel tvivm;

        [TestInitialize]
        public void TestInit()
        {
            tvivm = new TestTreeViewItemViewModel(services);
        }

        [TestMethod]
        public void Constructor_Initializes()
        {
            Assert.AreSame(services, tvivm.Services);
            Assert.IsNotNull(tvivm.CloudFoundryService);
        }

        [TestMethod]
        public void Expansion_LoadsChildren_WhenNotAlreadyExpanded()
        {
            Assert.IsFalse(tvivm.IsExpanded);
            int initialCalls = tvivm.NumTimesChildrenLoaded;

            tvivm.IsExpanded = true;

            Assert.AreEqual(initialCalls + 1, tvivm.NumTimesChildrenLoaded);
        }

        [TestMethod]
        public void Expansion_DoesNotLoadChildren_WhenAlreadyExpanded()
        {
            tvivm = new TestTreeViewItemViewModel(services)
            {
                IsExpanded = true
            };

            int initialCalls = tvivm.NumTimesChildrenLoaded;

            tvivm.IsExpanded = true;

            Assert.AreEqual(initialCalls, tvivm.NumTimesChildrenLoaded);
        }
    }

    class TestTreeViewItemViewModel : TreeViewItemViewModel
    {
        public TestTreeViewItemViewModel(IServiceProvider services) : base(null, services)
        {
            NumTimesChildrenLoaded = 0;
        }

        public int NumTimesChildrenLoaded { get; set; }

        protected override async Task LoadChildren()
        {
            NumTimesChildrenLoaded += 1;
        }
    }
}
