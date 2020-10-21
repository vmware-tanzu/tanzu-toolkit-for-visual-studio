using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace TanzuForVS.ViewModels
{
    [TestClass]
    public class TreeViewItemViewModelTests : ViewModelTestSupport
    {
        [TestMethod]
        public void Constructor_Initializes()
        {
            var vm = new TestTreeViewItemViewModel(services);
            Assert.AreSame(services, vm.Services);
            Assert.IsNotNull(vm.CloudFoundryService);
        }
    }

    class TestTreeViewItemViewModel : TreeViewItemViewModel
    {
        public TestTreeViewItemViewModel(IServiceProvider services) : base(null, services)
        {
        }
    }
}
