using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class AbstractViewModelTests : ViewModelTestSupport
    {
        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();
        }

        [TestMethod]
        public void Constructor_Initializes()
        {
            var vm = new TestAbstractViewModel(Services);
            Assert.AreSame(Services, vm.Services);
            Assert.IsNotNull(vm.DialogService);
            Assert.IsNotNull(vm.ViewLocatorService);
            Assert.IsNotNull(vm.CloudFoundryService);
            Assert.IsNull(vm.ActiveView);
        }
    }

    internal class TestAbstractViewModel : AbstractViewModel
    {
        public TestAbstractViewModel(IServiceProvider services) : base(services)
        {
        }
    }
}
