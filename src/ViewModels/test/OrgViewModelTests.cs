﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class OrgViewModelTests : ViewModelTestSupport
    {
        private OrgViewModel _sut;
        private List<string> _receivedEvents;

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            _sut = new OrgViewModel(FakeCfOrg, Services);

            _receivedEvents = new List<string>();
            _sut.PropertyChanged += (sender, e) =>
            {
                _receivedEvents.Add(e.PropertyName);
            };

            MockUiDispatcherService.Setup(mock => mock.
                RunOnUiThread(It.IsAny<Action>()))
                    .Callback<Action>(method =>
                    {
                        // Run whatever method is passed to MockUiDispatcherService.RunOnUiThread; do not delegate to the UI Dispatcher
                        method();
                    });
        }

        [TestCleanup]
        public void TestCleanup()
        {
            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public void Constructor_SetsDisplayTextToOrgName()
        {
            Assert.AreEqual(FakeCfOrg.OrgName, _sut.DisplayText);
        }

        [TestMethod]
        public void Constructor_SetsLoadingPlaceholder()
        {
            Assert.AreEqual(OrgViewModel._loadingMsg, _sut.LoadingPlaceholder.DisplayText);
        }

        [TestMethod]
        public void Constructor_SetsEmptyPlaceholder()
        {
            Assert.AreEqual(OrgViewModel._emptySpacesPlaceholderMsg, _sut.EmptyPlaceholder.DisplayText);
        }

        [TestMethod]
        public async Task LoadChildren_UpdatesAllSpaces()
        {
            var initialSpacesList = new System.Collections.ObjectModel.ObservableCollection<TreeViewItemViewModel>
            {
                new SpaceViewModel(new CloudFoundrySpace("initial space 1", "initial space 1 guid", null), Services),
                new SpaceViewModel(new CloudFoundrySpace("initial space 2", "initial space 2 guid", null), Services),
                new SpaceViewModel(new CloudFoundrySpace("initial space 3", "initial space 3 guid", null), Services),
            };

            var newSpacesList = new List<CloudFoundrySpace>
            {
                new CloudFoundrySpace("initial space 1", "initial space 1 guid", null),
                new CloudFoundrySpace("initial space 2", "initial space 2 guid", null),
            };
            var fakeSucccessResponse = new DetailedResult<List<CloudFoundrySpace>>(
                content: newSpacesList,
                succeeded: true,
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            _sut.Children = initialSpacesList;

            /* erase record of initial "Children" event */
            _receivedEvents.Clear();

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(It.IsAny<CloudFoundryOrganization>(), true))
                    .ReturnsAsync(fakeSucccessResponse);

            Assert.AreEqual(initialSpacesList.Count, _sut.Children.Count);

            await _sut.LoadChildren();

            Assert.AreEqual(newSpacesList.Count, _sut.Children.Count);

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("Children", _receivedEvents[0]);

            Assert.IsFalse(_sut.HasEmptyPlaceholder);
        }

        [TestMethod]
        public async Task LoadChildren_AssignsNoSpacesPlaceholder_WhenThereAreNoSpaces()
        {
            var fakeNoSpacesResponse = new DetailedResult<List<CloudFoundrySpace>>(
                content: EmptyListOfSpaces,
                succeeded: true,
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(It.IsAny<CloudFoundryOrganization>(), true))
                    .ReturnsAsync(fakeNoSpacesResponse);

            await _sut.LoadChildren();

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());
            Assert.AreEqual(OrgViewModel._emptySpacesPlaceholderMsg, _sut.Children[0].DisplayText);

            Assert.AreEqual(_sut.EmptyPlaceholder, _sut.Children[0]);
            Assert.IsTrue(_sut.HasEmptyPlaceholder);

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("Children", _receivedEvents[0]);
        }

        [TestMethod]
        public async Task LoadChildren_SetsIsLoadingToFalse_WhenComplete()
        {
            var fakeNoSpacesResponse = new DetailedResult<List<CloudFoundrySpace>>(
                content: EmptyListOfSpaces,
                succeeded: true,
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(_sut.Org, true))
                    .ReturnsAsync(fakeNoSpacesResponse);

            _sut.IsLoading = true;

            await _sut.LoadChildren();

            Assert.IsFalse(_sut.IsLoading);
        }

        [TestMethod]
        public async Task LoadChildren_DisplaysErrorDialog_WhenSpacesRequestFails()
        {
            var fakeFailedResult = new DetailedResult<List<CloudFoundrySpace>>(
                succeeded: false,
                content: null,
                explanation: "junk",
                cmdDetails: FakeFailureCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
              GetSpacesForOrgAsync(_sut.Org, true))
                .ReturnsAsync(fakeFailedResult);

            await _sut.LoadChildren();

            Assert.IsFalse(_sut.IsLoading);

            MockDialogService.Verify(mock => mock.
              DisplayErrorDialog(OrgViewModel._getSpacesFailureMsg, fakeFailedResult.Explanation),
                Times.Once);
        }

        [TestMethod]
        public async Task FetchChildren_ReturnsListOfSpaces_WithoutUpdatingChildren()
        {
            List<CloudFoundrySpace> fakeSpacesList = new List<CloudFoundrySpace>
            {
                new CloudFoundrySpace("fake space name 1", "fake space id 1", FakeCfOrg),
                new CloudFoundrySpace("fake space name 2", "fake space id 2", FakeCfOrg),
            };

            var fakeSuccessResponse = new DetailedResult<List<CloudFoundrySpace>>(
                content: fakeSpacesList,
                succeeded: true,
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(FakeCfOrg, true))
                    .ReturnsAsync(fakeSuccessResponse);

            /* pre-check presence of placeholder */
            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());

            var spaces = await _sut.FetchChildren();

            Assert.AreEqual(2, spaces.Count);

            /* confirm presence of placeholder */
            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());

            // property changed events should not be raised
            Assert.AreEqual(0, _receivedEvents.Count);
        }

        [TestMethod]
        public async Task FetchChildren_DisplaysErrorDialog_WhenSpacesRequestFails()
        {
            var fakeFailedResult = new DetailedResult<List<CloudFoundrySpace>>(
                succeeded: false,
                content: null,
                explanation: "junk",
                cmdDetails: FakeFailureCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
              GetSpacesForOrgAsync(_sut.Org, true))
                .ReturnsAsync(fakeFailedResult);

            var result = await _sut.FetchChildren();

            CollectionAssert.AreEqual(EmptyListOfSpaces, result);

            MockDialogService.Verify(mock => mock.
              DisplayErrorDialog(OrgViewModel._getSpacesFailureMsg, fakeFailedResult.Explanation),
                Times.Once);
        }
        
        [TestMethod]
        [TestCategory("RefreshChildren")]
        public async Task RefreshOrg_UpdatesChildrenOnOrgViewModel()
        {
            var fakeSpaceName1 = "fake space 1";
            var fakeSpaceName2 = "fake space 2";
            var fakeSpaceName3 = "fake space 3";
            var fakeSpaceName4 = "fake space 4";

            var fakeSpaceGuid1 = "fake space 1";
            var fakeSpaceGuid2 = "fake space 2";
            var fakeSpaceGuid3 = "fake space 3";
            var fakeSpaceGuid4 = "fake space 4";

            _sut.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                // to be removed:
                new SpaceViewModel(new CloudFoundrySpace(fakeSpaceName1, fakeSpaceGuid1, _sut.Org), Services), 
                // to stay:
                new SpaceViewModel(new CloudFoundrySpace(fakeSpaceName2, fakeSpaceGuid2, _sut.Org), Services, expanded: true), // should stay expanded after refresh 
                new SpaceViewModel(new CloudFoundrySpace(fakeSpaceName3, fakeSpaceGuid3, _sut.Org), Services, expanded: false), // should stay collapsed after refresh 
            };

            var fakeSpacesList = new List<CloudFoundrySpace>
            {
                // original:
                new CloudFoundrySpace(fakeSpaceName2, fakeSpaceGuid2, _sut.Org),
                new CloudFoundrySpace(fakeSpaceName3, fakeSpaceGuid3, _sut.Org),
                // new:
                new CloudFoundrySpace(fakeSpaceName4, fakeSpaceGuid4, _sut.Org),
            };

            var fakeSuccessResult = new DetailedResult<List<CloudFoundrySpace>>(succeeded: true, content: fakeSpacesList);

            Assert.AreEqual(3, _sut.Children.Count);
            SpaceViewModel initialChildSpace1 = (SpaceViewModel)_sut.Children[0];
            SpaceViewModel initialChildSpace2 = (SpaceViewModel)_sut.Children[1];
            SpaceViewModel initialChildSpace3 = (SpaceViewModel)_sut.Children[2];
            Assert.AreEqual(fakeSpaceName1, initialChildSpace1.Space.SpaceName);
            Assert.AreEqual(fakeSpaceName2, initialChildSpace2.Space.SpaceName);
            Assert.AreEqual(fakeSpaceName3, initialChildSpace3.Space.SpaceName);
            Assert.IsFalse(initialChildSpace1.IsExpanded);
            Assert.IsTrue(initialChildSpace2.IsExpanded);
            Assert.IsFalse(initialChildSpace3.IsExpanded);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(_sut.Org, true))
                    .ReturnsAsync(fakeSuccessResult);

            _receivedEvents.Clear();

            await _sut.RefreshChildren();

            Assert.AreEqual(3, _sut.Children.Count);
            SpaceViewModel refreshedChildSpace1 = (SpaceViewModel)_sut.Children[0];
            SpaceViewModel refreshedChildSpace2 = (SpaceViewModel)_sut.Children[1];
            SpaceViewModel refreshedChildSpace3 = (SpaceViewModel)_sut.Children[2];
            Assert.AreEqual(fakeSpaceName2, refreshedChildSpace1.Space.SpaceName);
            Assert.AreEqual(fakeSpaceName3, refreshedChildSpace2.Space.SpaceName);
            Assert.AreEqual(fakeSpaceName4, refreshedChildSpace3.Space.SpaceName);
            Assert.IsTrue(refreshedChildSpace1.IsExpanded); // children that aren't new shouldn't change expansion
            Assert.IsFalse(refreshedChildSpace2.IsExpanded); // children that aren't new shouldn't change expansion
            Assert.IsFalse(refreshedChildSpace2.IsExpanded); // new children should start collapsed

            // property changed events should not be raised
            Assert.AreEqual(0, _receivedEvents.Count);

            MockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("RefreshChildren")]
        public async Task RefreshChildren_AddsPlaceholder_WhenAllSpacesAreRemoved()
        {
            var fakeInitialSpace = new CloudFoundrySpace("fake space name", "fake space id", _sut.Org);
            var svm = new SpaceViewModel(fakeInitialSpace, Services);

            var fakeNoSpacesResult = new DetailedResult<List<CloudFoundrySpace>>(
                succeeded: true,
                content: new List<CloudFoundrySpace>(), // simulate org having lost all spaces before refresh
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(_sut.Org, true))
                    .ReturnsAsync(fakeNoSpacesResult);

            _sut.Children = new ObservableCollection<TreeViewItemViewModel> { svm }; // simulate org initially having 1 space child

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(SpaceViewModel), _sut.Children[0].GetType());

            await _sut.RefreshChildren();

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());
            Assert.AreEqual(OrgViewModel._emptySpacesPlaceholderMsg, _sut.Children[0].DisplayText);
        }

        [TestMethod]
        [TestCategory("RefreshChildren")]
        public async Task RefreshChildren_RemovesPlaceholder_WhenEmptyOrgGainsChildren()
        {
            // simulate org initially having no space children
            _sut.Children = new ObservableCollection<TreeViewItemViewModel> { _sut.EmptyPlaceholder };
            _sut.HasEmptyPlaceholder = true;

            var fakeNewSpace = new CloudFoundrySpace("fake space name", "fake space id", _sut.Org);

            var fakeSuccessfulSpacesResult = new DetailedResult<List<CloudFoundrySpace>>(
                succeeded: true,
                content: new List<CloudFoundrySpace>
                {
                    fakeNewSpace, // simulate org having gained a space child before refresh
                },
                explanation: null,
                cmdDetails: FakeSuccessCmdResult);

            MockCloudFoundryService.Setup(mock => mock.
                GetSpacesForOrgAsync(_sut.Org, true))
                    .ReturnsAsync(fakeSuccessfulSpacesResult);

            _sut.Children = new ObservableCollection<TreeViewItemViewModel> { _sut.EmptyPlaceholder }; // simulate org initially having no space children

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(PlaceholderViewModel), _sut.Children[0].GetType());
            Assert.AreEqual(OrgViewModel._emptySpacesPlaceholderMsg, _sut.Children[0].DisplayText);

            await _sut.RefreshChildren();

            Assert.AreEqual(1, _sut.Children.Count);
            Assert.AreEqual(typeof(SpaceViewModel), _sut.Children[0].GetType());
        }

    }
}
