using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace TanzuForVS.ViewModels
{
    [TestClass]
    public class AbstractViewModelTests : ViewModelTestSupport
    {
        [TestInitialize]
        public void TestInit()
        {
        }

        [TestMethod]
        public void Constructor_Initializes()
        {
            var vm = new TestAbstractViewModel(services);
            Assert.AreSame(services, vm.Services);
            Assert.IsNotNull(vm.DialogService);
            Assert.IsNotNull(vm.ViewLocatorService);
            Assert.IsNotNull(vm.CloudFoundryService);
            Assert.IsFalse(vm.IsLoggedIn);
            Assert.IsNull(vm.ActiveView);
        }
    }

    class TestAbstractViewModel : AbstractViewModel
    {
        public TestAbstractViewModel(IServiceProvider services) : base(services)
        {
        }
    }
}
