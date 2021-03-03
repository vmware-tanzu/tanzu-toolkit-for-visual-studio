using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.VisualStudio.ViewModels.Tests
{
    [TestClass]
    public class TreeViewItemViewModelTests : ViewModelTestSupport
    {
        TestTreeViewItemViewModel collpased_tvivm;
        TestTreeViewItemViewModel expanded_tvivm;

        [TestInitialize]
        public void TestInit()
        {
            collpased_tvivm = new TestTreeViewItemViewModel(services);
            expanded_tvivm = new TestTreeViewItemViewModel(services)
            {
                IsExpanded = true
            };
        }

        [TestMethod]
        public void Constructor_Initializes()
        {
            var sut = new TestTreeViewItemViewModel(services);

            Assert.AreSame(services, sut.Services);
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
            var sut = new TestTreeViewItemViewModel(services, childless: true);

            Assert.IsNull(sut.LoadingPlaceholder);
            Assert.IsNull(sut.Children);
        }

        [TestMethod]
        public void Expansion_SetsIsLoadingToTrue_WhenNotAlreadyExpanded_AndWhenNotLoading()
        {
            var sut = collpased_tvivm;

            Assert.IsFalse(sut.IsExpanded);
            Assert.IsFalse(sut.IsLoading);

            sut.IsExpanded = true;

            Assert.IsTrue(sut.IsLoading);
        }

        [TestMethod]
        public async Task Expansion_LoadsChildren_WhenNotAlreadyExpanded_AndWhenNotLoading()
        {
            var sut = collpased_tvivm;
            int initialCalls = sut.NumTimesChildrenLoaded;

            Assert.IsFalse(sut.IsExpanded);
            Assert.IsFalse(sut.IsLoading);

            await Task.Run(() => { sut.IsExpanded = true; });

            bool loadChildrenWasCalledOnce = sut.NumTimesChildrenLoaded == initialCalls + 1;
            Assert.IsTrue(loadChildrenWasCalledOnce);
        }

        [TestMethod]
        public async Task Expansion_DoesNotLoadChildren_WhenAlreadyExpanded()
        {
            var sut = expanded_tvivm;
            int initialCalls = sut.NumTimesChildrenLoaded;

            Assert.IsTrue(sut.IsExpanded);

            await Task.Run(() => { sut.IsExpanded = true; });

            bool loadChildrenWasNeverCalled = sut.NumTimesChildrenLoaded == initialCalls;
            Assert.IsTrue(loadChildrenWasNeverCalled);
        }
        
        [TestMethod]
        public async Task Expansion_DoesNotLoadChildren_WhenAlreadyLoading()
        {
            var sut = collpased_tvivm;
            int initialCalls = sut.NumTimesChildrenLoaded;

            sut.IsLoading = true;

            await Task.Run(() => { sut.IsExpanded = true; });

            bool loadChildrenWasNeverCalled = sut.NumTimesChildrenLoaded == initialCalls;
            Assert.IsTrue(loadChildrenWasNeverCalled);
        }

        [TestMethod]
        public void Expansion_ReplacesChildrenWithLoadingPlaceholder_WhileChildrenLoad()
        {
            var sut = collpased_tvivm;
            sut.Children = new ObservableCollection<TreeViewItemViewModel>()
            {
                null,
                null,
                null
            };

            Assert.AreEqual(3, sut.Children.Count);

            sut.IsExpanded = true;

            Assert.AreEqual(1, sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), sut.Children[0].GetType());
            Assert.AreEqual(sut.LoadingPlaceholder, sut.Children[0]);
            Assert.AreEqual(TreeViewItemViewModel._defaultLoadingMsg, sut.Children[0].DisplayText);
        }
    }

    class TestTreeViewItemViewModel : TreeViewItemViewModel
    {
        public TestTreeViewItemViewModel(IServiceProvider services) : base(null, services)
        {
            NumTimesChildrenLoaded = 0;
        }
        public TestTreeViewItemViewModel(IServiceProvider services, bool childless) : base(null, services, childless)
        {
            NumTimesChildrenLoaded = 0;
        }

        public int NumTimesChildrenLoaded { get; set; }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal protected override async Task LoadChildren()
        {
            NumTimesChildrenLoaded += 1;
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
