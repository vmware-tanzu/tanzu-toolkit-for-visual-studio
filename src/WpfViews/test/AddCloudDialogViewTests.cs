﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.WpfViews.Commands;
using Moq;

namespace Tanzu.Toolkit.WpfViews.Tests
{
    [TestClass]
    public class AddCloudDialogViewTests : ViewTestSupport
    {
        private AddCloudDialogViewModel vm;

        [TestInitialize]
        public void TestInit()
        {
            vm = new AddCloudDialogViewModel(services);
        }

        [TestMethod]
        public void Constructor_Initializes()
        {
            vm.InstanceName = "My CF";
            vm.Target = "http://test/";
            var view = new AddCloudDialogView(vm, mockThemeService.Object);

            // Verify DataContext initalized
            Assert.AreSame(vm, view.DataContext);

            // Verify Login command points to view model
            var command = view.AddCloudCommand as AsyncDelegatingCommand;
            Assert.IsNotNull(command);
            Assert.AreEqual(vm, command.Action.Target);

            // Verify ViewModel callback for password
            Assert.IsNotNull(vm.GetPassword);

            // Verify bindings
            Assert.AreEqual(vm.InstanceName, view.tbInstanceName.Text);
            Assert.AreEqual(vm.Target, view.tbUrl.Text);
        }

        [TestMethod]
        public void Constructor_SetsWindowTheme_UsingThemeService()
        {
            var view = new AddCloudDialogView(vm, mockThemeService.Object);
            mockThemeService.Verify(mock => mock.SetTheme(view), Times.Once);
        }
    }
}
