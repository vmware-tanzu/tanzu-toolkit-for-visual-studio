using Microsoft.VisualStudio.TestTools.UnitTesting;
using TanzuForVS.ViewModels;
using TanzuForVS.WpfViews.Commands;

namespace TanzuForVS.WpfViews.Tests
{
    [TestClass]
    public class AddCloudDialogViewTests : ViewTestSupport
    {
        [TestInitialize]
        public void TestInit()
        {
        }

        [TestMethod]
        public void Constructor_Initializes()
        {
            var vm = new AddCloudDialogViewModel(services);
            vm.InstanceName = "My CF";
            vm.Target = "http://test/";
            var view = new AddCloudDialogView(vm);

            // Verify DataContext initalized
            Assert.AreSame(vm, view.DataContext);

            // Verify Login command points to view model
            var command = view.AddCloudCommand as AsyncDelegatingCommand;
            Assert.IsNotNull(command);
            Assert.AreEqual(vm, command.action.Target);

            // Verify ViewModel callback for password
            Assert.IsNotNull(vm.GetPassword);

            // Verify bindings
            Assert.AreEqual(vm.InstanceName, view.tbInstanceName.Text);
            Assert.AreEqual(vm.Target, view.tbUrl.Text);
        }
    }
}
