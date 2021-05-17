using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class TreeViewItemViewModelTests : ViewModelTestSupport
    {
        private TestTreeViewItemViewModel _collpasedTvivm;
        private TestTreeViewItemViewModel _expandedTvivm;

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            _collpasedTvivm = new TestTreeViewItemViewModel(Services);
            _expandedTvivm = new TestTreeViewItemViewModel(Services)
            {
                IsExpanded = true,
            };

            // ignore first mock task invocation caused by initial expansion
            MockThreadingService.Invocations.Clear();
        }

        [TestMethod]
        public void Constructor_Initializes()
        {
            var sut = new TestTreeViewItemViewModel(Services);

            Assert.AreSame(Services, sut.Services);
            Assert.IsNotNull(sut.CloudFoundryService);

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
        public void Expansion_SetsIsLoadingToTrue_WhenNotAlreadyExpanded_AndWhenNotLoading()
        {
            var sut = _collpasedTvivm;

            Assert.IsFalse(sut.IsExpanded);
            Assert.IsFalse(sut.IsLoading);

            sut.IsExpanded = true;

            Assert.IsTrue(sut.IsLoading);
        }

        [TestMethod]
        public void Expansion_LoadsChildren_WhenNotAlreadyExpanded_AndWhenNotLoading()
        {
            var sut = _collpasedTvivm;

            Assert.IsFalse(sut.IsExpanded);
            Assert.IsFalse(sut.IsLoading);

            sut.IsExpanded = true;

            MockThreadingService.Verify(m => m.StartTask(It.IsAny<Func<Task>>()), Times.Once);
        }

        [TestMethod]
        public void Expansion_DoesNotLoadChildren_WhenAlreadyExpanded()
        {
            var sut = _expandedTvivm;

            Assert.IsTrue(sut.IsExpanded);

            sut.IsExpanded = true;

            MockThreadingService.Verify(m => m.StartTask(It.IsAny<Func<Task>>()), Times.Never);
        }

        [TestMethod]
        public void Expansion_DoesNotLoadChildren_WhenAlreadyLoading()
        {
            var sut = _collpasedTvivm;

            sut.IsLoading = true;

            sut.IsExpanded = true;

            MockThreadingService.Verify(m => m.StartTask(It.IsAny<Func<Task>>()), Times.Never);
        }

        [TestMethod]
        public void Expansion_ReplacesChildrenWithLoadingPlaceholder_WhileChildrenLoad()
        {
            var sut = _collpasedTvivm;
            sut.Children = new ObservableCollection<TreeViewItemViewModel>()
            {
                null,
                null,
                null,
            };

            Assert.AreEqual(3, sut.Children.Count);

            sut.IsExpanded = true;

            Assert.AreEqual(1, sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), sut.Children[0].GetType());
            Assert.AreEqual(sut.LoadingPlaceholder, sut.Children[0]);
            Assert.AreEqual(TreeViewItemViewModel._defaultLoadingMsg, sut.Children[0].DisplayText);
        }
    }

    internal class TestTreeViewItemViewModel : TreeViewItemViewModel
    {

        public TestTreeViewItemViewModel(IServiceProvider services) : base(null, services)
        {
        }

        public TestTreeViewItemViewModel(IServiceProvider services, bool childless) : base(null, services, childless)
        {
        }

        internal protected override async Task LoadChildren()
        {
            await Task.Run(() => { });
        }
    }
}
