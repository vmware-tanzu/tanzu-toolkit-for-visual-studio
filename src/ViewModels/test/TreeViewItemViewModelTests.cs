using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class TreeViewItemViewModelTests : ViewModelTestSupport
    {
        private TestTreeViewItemViewModel _collpasedTvivm;

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            _collpasedTvivm = new TestTreeViewItemViewModel(Services);

            // ignore first mock task invocation caused by initial expansion
            MockThreadingService.Invocations.Clear();
        }

        [TestMethod]
        public void Constructor_Initializes()
        {
            var sut = new TestTreeViewItemViewModel(Services);

            Assert.AreSame(Services, sut.Services);
            Assert.IsFalse(sut.IsLoading);

            /* loading placeholder gets instantiated */
            Assert.IsNotNull(sut.LoadingPlaceholder);
            Assert.AreEqual(TreeViewItemViewModel._defaultLoadingMsg, sut.LoadingPlaceholder.DisplayText);

            /* children set to loading placeholder */
            Assert.AreEqual(1, sut.Children.Count);
            Assert.AreEqual(sut.LoadingPlaceholder, sut.Children[0]);
        }

        [TestMethod]
        public void Constructor_DoesNotCreatePlaceholder_OrSetChildren_WhenMarkedAsChildless()
        {
            var sut = new TestTreeViewItemViewModel(Services, childless: true);

            Assert.IsNull(sut.LoadingPlaceholder);
            Assert.IsNull(sut.Children);
        }

        [TestMethod]
        public void Expansion_BeginsUpdatingChildrenOnBackgroundThread_WhenNotAlreadyLoading()
        {
            var sut = _collpasedTvivm;
            MockThreadingService.Setup(m => m.StartBackgroundTask(It.IsAny<Func<Task>>())).Verifiable();

            Assert.IsFalse(sut.IsExpanded);
            Assert.IsFalse(sut.IsLoading);

            sut.IsExpanded = true;

            Assert.IsTrue(sut.IsExpanded);
            MockThreadingService.Verify(m => m.StartBackgroundTask(sut.UpdateAllChildren), Times.Once);
        }

        [TestMethod]
        public void Expansion_DoesNotInvokeUpdateAllChildren_WhenAlreadyLoading()
        {
            var sut = _collpasedTvivm;
            sut.IsLoading = true;

            MockThreadingService.Setup(m => m.StartBackgroundTask(It.IsAny<Func<Task>>())).Verifiable();

            Assert.IsFalse(sut.IsExpanded);
            Assert.IsTrue(sut.IsLoading);

            sut.IsExpanded = true;

            Assert.IsTrue(sut.IsExpanded);
            MockThreadingService.Verify(m => m.StartBackgroundTask(It.IsAny<Func<Task>>()), Times.Never);
        }
    }

    internal class TestTreeViewItemViewModel : TreeViewItemViewModel
    {
        public TestTreeViewItemViewModel(IServiceProvider services) : base(null, null, services)
        {
        }

        public TestTreeViewItemViewModel(IServiceProvider services, bool childless) : base(null, null, services, childless)
        {
        }
    }
}