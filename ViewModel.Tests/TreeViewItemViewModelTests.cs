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
            var sut = collpased_tvivm;

            Assert.AreSame(services, sut.Services);
            Assert.IsNotNull(sut.CloudFoundryService);

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
        public async Task Expansion_LoadsChildren_WhenNotAlreadyExpanded()
        {
            Assert.IsFalse(collpased_tvivm.IsExpanded);
            int initialCalls = collpased_tvivm.NumTimesChildrenLoaded;

            await Task.Run(() => { collpased_tvivm.IsExpanded = true; });

            Assert.AreEqual(initialCalls + 1, collpased_tvivm.NumTimesChildrenLoaded);
        }

        [TestMethod]
        public async Task Expansion_DoesNotLoadChildren_WhenAlreadyExpanded()
        {
            int initialCalls = expanded_tvivm.NumTimesChildrenLoaded;

            await Task.Run(() => { collpased_tvivm.IsExpanded = true; });

            Assert.AreEqual(initialCalls, collpased_tvivm.NumTimesChildrenLoaded);
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
