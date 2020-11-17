using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using TanzuForVS.Models;

namespace TanzuForVS.ViewModels
{
    [TestClass]
    public class CloudExplorerViewModelTests : ViewModelTestSupport
    {
        private CloudExplorerViewModel vm;
        List<string> receivedEvents;

        [TestInitialize]
        public void TestInit()
        {
            mockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances).Returns(new Dictionary<string, CloudFoundryInstance>());
            vm = new CloudExplorerViewModel(services);
            receivedEvents = new List<string>();
        }


        [TestMethod]
        public void CanOpenLoginView_ReturnsExpected()
        {
            Assert.IsTrue(vm.CanOpenLoginView(null));
        }

        [TestMethod]
        public void CanStopCfApp_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanStopCfApp(null));
        }

        [TestMethod]
        public void CanStartCfApp_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanStartCfApp(null));
        }

        [TestMethod]
        public void CanDeleteCfApp_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanDeleteCfApp(null));

        }

        [TestMethod]
        public void CanRefreshCfInstance_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanRefreshCfInstance(null));
        }

        [TestMethod]
        public void CanRefreshOrg_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanRefreshOrg(null));
        }

        [TestMethod]
        public void CanRefreshSpace_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanRefreshSpace(null));
        }

        [TestMethod]
        public void CanRefreshApp_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanRefreshApp(null));
        }

        [TestMethod]
        public void CanRefreshAllCloudConnections_ReturnsTrue()
        {
            Assert.IsTrue(vm.CanRefreshAllCloudConnections(null));
        }


        [TestMethod]
        public void OpenLoginView_CallsDialogService_ShowDialog()
        {
            vm.OpenLoginView(null);
            mockDialogService.Verify(ds => ds.ShowDialog(typeof(AddCloudDialogViewModel).Name, null), Times.Once);
        }

        [TestMethod]
        public void OpenLoginView_UpdatesCloudFoundryInstances_AfterDialogCloses()
        {
            var fakeCfsDict = new Dictionary<string, CloudFoundryInstance>
            {
                { "fake cf", new CloudFoundryInstance("fake cf", null, null) }
            };

            Assert.AreEqual(0, vm.CloudFoundryList.Count);

            mockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances).Returns(fakeCfsDict);

            vm.OpenLoginView(null);

            Assert.IsTrue(vm.HasCloudTargets);
            Assert.AreEqual(1, vm.CloudFoundryList.Count);
        }

        [TestMethod]
        public async Task StopCfApp_CallsStopCfAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null);

            mockCloudFoundryService.Setup(mock => mock.StopAppAsync(fakeApp)).ReturnsAsync(true);

            Exception shouldStayNull = null;
            try
            {
                await vm.StopCfApp(fakeApp);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task StartCfApp_CallsStartAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null);

            mockCloudFoundryService.Setup(mock => mock.StartAppAsync(fakeApp)).ReturnsAsync(true);

            Exception shouldStayNull = null;
            try
            {
                await vm.StartCfApp(fakeApp);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task DeleteCfApp_CallsDeleteAppAsync()
        {
            var fakeApp = new CloudFoundryApp("junk", "junk", parentSpace: null);

            mockCloudFoundryService.Setup(mock => mock.DeleteAppAsync(fakeApp)).ReturnsAsync(true);

            Exception shouldStayNull = null;
            try
            {
                await vm.DeleteCfApp(fakeApp);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public void RefreshApp_RaisesPropertyChangedEventForIsStopped()
        {
            CloudFoundryApp fakeApp = new CloudFoundryApp("fake app name", "fake app guid", null);
            var avm = new AppViewModel(fakeApp, services);

            avm.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            vm.RefreshApp(avm);

            Assert.AreEqual(1, receivedEvents.Count);
            Assert.AreEqual("IsStopped", receivedEvents[0]);
        }

        [TestMethod]
        public async Task RefreshCfInstance_UpdatesChildrenOnCfInstanceViewModel()
        {
            var fakeCfInstance = new CloudFoundryInstance("fake space name", "fake space id", null);
            var fakeCfInstanceViewModel = new CfInstanceViewModel(fakeCfInstance, services);

            var fakeOrgName1 = "fake org 1";
            var fakeOrgName2 = "fake org 2";
            var fakeOrgName3 = "fake org 3";

            var fakeOrgGuid1 = "fake org 1";
            var fakeOrgGuid2 = "fake org 2";
            var fakeOrgGuid3 = "fake org 3";

            fakeCfInstanceViewModel.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new OrgViewModel(new CloudFoundryOrganization(fakeOrgName1, fakeOrgGuid1, fakeCfInstance), services)
            };

            fakeCfInstanceViewModel.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            Assert.AreEqual(1, fakeCfInstanceViewModel.Children.Count);
            OrgViewModel firstChildOrg = (OrgViewModel)fakeCfInstanceViewModel.Children[0];
            Assert.AreEqual(fakeOrgName1, firstChildOrg.Org.OrgName);

            mockCloudFoundryService.Setup(mock => mock.GetOrgsForCfInstanceAsync(fakeCfInstance)).ReturnsAsync(new List<CloudFoundryOrganization>
            {
                new CloudFoundryOrganization(fakeOrgName2, fakeOrgGuid2, fakeCfInstance),
                new CloudFoundryOrganization(fakeOrgName3, fakeOrgGuid3, fakeCfInstance)
            });

            await vm.RefreshCfInstance(fakeCfInstanceViewModel);

            Assert.AreEqual(2, fakeCfInstanceViewModel.Children.Count);
            OrgViewModel firstNewChildOrg = (OrgViewModel)fakeCfInstanceViewModel.Children[0];
            OrgViewModel secondNewChildOrg = (OrgViewModel)fakeCfInstanceViewModel.Children[1];
            Assert.AreEqual(fakeOrgName2, firstNewChildOrg.Org.OrgName);
            Assert.AreEqual(fakeOrgName3, secondNewChildOrg.Org.OrgName);

            // property changed events should not be raised
            Assert.AreEqual(0, receivedEvents.Count);

            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task RefreshOrg_UpdatesChildrenOnOrgViewModel()
        {
            var fakeOrg = new CloudFoundryOrganization("fake org name", "fake org id", null);
            var fakeOrgViewModel = new OrgViewModel(fakeOrg, services);

            var fakeSpaceName1 = "fake space 1";
            var fakeSpaceName2 = "fake space 2";
            var fakeSpaceName3 = "fake space 3";

            var fakeSpaceGuid1 = "fake space 1";
            var fakeSpaceGuid2 = "fake space 2";
            var fakeSpaceGuid3 = "fake space 3";

            fakeOrgViewModel.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new SpaceViewModel(new CloudFoundrySpace(fakeSpaceName1, fakeSpaceGuid1, fakeOrg), services)
            };

            fakeOrgViewModel.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            Assert.AreEqual(1, fakeOrgViewModel.Children.Count);
            SpaceViewModel firstChildSpace = (SpaceViewModel)fakeOrgViewModel.Children[0];
            Assert.AreEqual(fakeSpaceName1, firstChildSpace.Space.SpaceName);

            mockCloudFoundryService.Setup(mock => mock.GetSpacesForOrgAsync(fakeOrg)).ReturnsAsync(new List<CloudFoundrySpace>
            {
                new CloudFoundrySpace(fakeSpaceName2, fakeSpaceGuid2, fakeOrg),
                new CloudFoundrySpace(fakeSpaceName3, fakeSpaceGuid3, fakeOrg)
            });

            await vm.RefreshOrg(fakeOrgViewModel);

            Assert.AreEqual(2, fakeOrgViewModel.Children.Count);
            SpaceViewModel firstNewChildSpace = (SpaceViewModel)fakeOrgViewModel.Children[0];
            SpaceViewModel secondNewChildSpace = (SpaceViewModel)fakeOrgViewModel.Children[1];
            Assert.AreEqual(fakeSpaceName2, firstNewChildSpace.Space.SpaceName);
            Assert.AreEqual(fakeSpaceName3, secondNewChildSpace.Space.SpaceName);

            // property changed events should not be raised
            Assert.AreEqual(0, receivedEvents.Count);

            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task RefreshSpace_UpdatesChildrenOnSpaceViewModel()
        {
            var fakeSpace = new CloudFoundrySpace("fake space name", "fake space id", null);
            var fakeSpaceViewModel = new SpaceViewModel(fakeSpace, services);

            var fakeAppName1 = "fake app 1";
            var fakeAppName2 = "fake app 2";
            var fakeAppName3 = "fake app 3";

            var fakeAppGuid1 = "fake app 1";
            var fakeAppGuid2 = "fake app 2";
            var fakeAppGuid3 = "fake app 3";

            fakeSpaceViewModel.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                new AppViewModel(new CloudFoundryApp(fakeAppName1, fakeAppGuid1, fakeSpace), services)
            };

            fakeSpaceViewModel.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            Assert.AreEqual(1, fakeSpaceViewModel.Children.Count);
            AppViewModel firstChildApp = (AppViewModel)fakeSpaceViewModel.Children[0];
            Assert.AreEqual(fakeAppName1, firstChildApp.App.AppName);

            mockCloudFoundryService.Setup(mock => mock.GetAppsForSpaceAsync(fakeSpace)).ReturnsAsync(new List<CloudFoundryApp>
            {
                new CloudFoundryApp(fakeAppName2, fakeAppGuid2, fakeSpace),
                new CloudFoundryApp(fakeAppName3, fakeAppGuid3, fakeSpace)
            });

            await vm.RefreshSpace(fakeSpaceViewModel);

            Assert.AreEqual(2, fakeSpaceViewModel.Children.Count);
            AppViewModel firstNewChildApp = (AppViewModel)fakeSpaceViewModel.Children[0];
            AppViewModel secondNewChildApp = (AppViewModel)fakeSpaceViewModel.Children[1];
            Assert.AreEqual(fakeAppName2, firstNewChildApp.App.AppName);
            Assert.AreEqual(fakeAppName3, secondNewChildApp.App.AppName);

            // property changed events should not be raised
            Assert.AreEqual(0, receivedEvents.Count);

            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_UpdatesCfListWithInstancesFromCfService()
        {
            var fakeCfInstance1 = new CloudFoundryInstance("fake cf name1", "http://fake1.api.address", "fake-token1");
            var fakeCfInstance2 = new CloudFoundryInstance("fake cf name2", "http://fake2.api.address", "fake-token2");
            var fakeCfInstance3 = new CloudFoundryInstance("fake cf name3", "http://fake3.api.address", "fake-token3");

            mockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances)
                .Returns(new Dictionary<string, CloudFoundryInstance> { 
                    { "instance1", fakeCfInstance1 },
                    { "instance2", fakeCfInstance2 },
                    { "instance3", fakeCfInstance3 },
                });
            mockCloudFoundryService.Setup(mock => mock.GetOrgsForCfInstanceAsync(It.IsAny<CloudFoundryInstance>())).ReturnsAsync(
               new List<CloudFoundryOrganization> { });

            vm.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            var listCount = vm.CloudFoundryList.Count;
            Assert.AreEqual(0, listCount);

            await vm.RefreshAllCloudConnections(null);

            var newListCount = vm.CloudFoundryList.Count;

            // ensure the refresh method actually queried the dictionary
            mockCloudFoundryService.VerifyAll();

            Assert.AreEqual(3, newListCount);

            Assert.AreEqual(1, receivedEvents.Count);
            Assert.AreEqual("HasCloudTargets", receivedEvents[0]);
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_RefreshesEachTreeViewItemViewModel()
        {
            var eventsRaisedByCFIVM = new List<string>(); 
            var eventsRaisedByOVM = new List<string>(); 
            var eventsRaisedBySVM = new List<string>(); 
            var eventsRaisedByAVM = new List<string>(); 

            var fakeCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");

            mockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances)
                .Returns(new Dictionary<string, CloudFoundryInstance> {
                    { "fake cf name", fakeCfInstance }
                });

            var cfivm = new CfInstanceViewModel(fakeCfInstance, services);
            cfivm.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                eventsRaisedByCFIVM.Add(e.PropertyName);
            };

            var fakeOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeCfInstance);
            var ovm = new OrgViewModel(fakeOrg, services);
            ovm.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                eventsRaisedByOVM.Add(e.PropertyName);
            };

            var fakeSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeOrg);
            var svm = new SpaceViewModel(fakeSpace, services);
            svm.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                eventsRaisedBySVM.Add(e.PropertyName);
            };

            var fakeApp = new CloudFoundryApp("fake app name", "fake app id", fakeSpace);
            var avm = new AppViewModel(fakeApp, services);
            avm.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                eventsRaisedByAVM.Add(e.PropertyName);
            };

            mockCloudFoundryService.Setup(mock => mock.GetOrgsForCfInstanceAsync(fakeCfInstance)).ReturnsAsync(
                new List<CloudFoundryOrganization> { fakeOrg });

            mockCloudFoundryService.Setup(mock => mock.GetSpacesForOrgAsync(fakeOrg)).ReturnsAsync(
                new List<CloudFoundrySpace> { fakeSpace });

            mockCloudFoundryService.Setup(mock => mock.GetAppsForSpaceAsync(fakeSpace)).ReturnsAsync(
                new List<CloudFoundryApp> { fakeApp });

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel>{ovm};

            ovm.Children = new ObservableCollection<TreeViewItemViewModel>{svm};

            svm.Children = new ObservableCollection<TreeViewItemViewModel>{avm};

            vm.CloudFoundryList = new List<CfInstanceViewModel>{cfivm};


            await vm.RefreshAllCloudConnections(null);

            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(ovm, cfivm.Children[0]);
            Assert.AreEqual(1, eventsRaisedByCFIVM.Count); 
            Assert.AreEqual("Children", eventsRaisedByCFIVM[0]);
            
            Assert.AreEqual(1, ovm.Children.Count);
            Assert.AreEqual(svm, ovm.Children[0]);
            Assert.AreEqual(1, eventsRaisedByOVM.Count); 
            Assert.AreEqual("Children", eventsRaisedByOVM[0]);
            
            Assert.AreEqual(1, svm.Children.Count);
            Assert.AreEqual(avm, svm.Children[0]);
            Assert.AreEqual(1, eventsRaisedBySVM.Count); 
            Assert.AreEqual("Children", eventsRaisedBySVM[0]);
            
            Assert.AreEqual(1, eventsRaisedByAVM.Count); 
            Assert.AreEqual("IsStopped", eventsRaisedByAVM[0]);

            // ensure all view models issued queries for updated lists of children
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_DoesNotThrowExceptions_WhenCfInstancesHaveNullChildren()
        {
            var fakeCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");

            mockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances)
                .Returns(new Dictionary<string, CloudFoundryInstance> {
                    { "fake cf name", fakeCfInstance }
                });

            var cfivm = new CfInstanceViewModel(fakeCfInstance, services);

            // check for presence of Dummy child (sanity check)
            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.IsNull(cfivm.Children[0]);

            mockCloudFoundryService.Setup(mock => mock.GetOrgsForCfInstanceAsync(fakeCfInstance)).ReturnsAsync(new List<CloudFoundryOrganization>());

            vm.CloudFoundryList = new List<CfInstanceViewModel>
            {
                cfivm
            };

            Exception shouldStayNull = null;
            try
            {
                await vm.RefreshAllCloudConnections(null);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);

            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.IsNull(cfivm.Children[0]);

            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_DoesNotThrowExceptions_WhenOrgsHaveNullChildren()
        {
            var fakeCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");

            mockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances)
                .Returns(new Dictionary<string, CloudFoundryInstance> {
                    { "fake cf name", fakeCfInstance }
                });

            var cfivm = new CfInstanceViewModel(fakeCfInstance, services);

            var fakeOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeCfInstance);
            var ovm = new OrgViewModel(fakeOrg, services);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                ovm
            };

            // check for presence of Dummy child (sanity check)
            Assert.AreEqual(1, ovm.Children.Count);
            Assert.IsNull(ovm.Children[0]);

            mockCloudFoundryService.Setup(mock => mock.GetOrgsForCfInstanceAsync(fakeCfInstance)).ReturnsAsync(new List<CloudFoundryOrganization>
            {
                fakeOrg
            });

            mockCloudFoundryService.Setup(mock => mock.GetSpacesForOrgAsync(fakeOrg)).ReturnsAsync(new List<CloudFoundrySpace>());

            vm.CloudFoundryList = new List<CfInstanceViewModel>
            {
                cfivm
            };

            Exception shouldStayNull = null;
            try
            {
                await vm.RefreshAllCloudConnections(null);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);

            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(ovm, cfivm.Children[0]);

            Assert.AreEqual(1, ovm.Children.Count);
            Assert.IsNull(ovm.Children[0]);

            mockCloudFoundryService.VerifyAll();

        }

        [TestMethod]
        public async Task RefreshAllCloudConnections_DoesNotThrowExceptions_WhenSpacesHaveNullChildren()
        {
            var fakeCfInstance = new CloudFoundryInstance("fake cf name", "http://fake.api.address", "fake-token");

            mockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances)
                .Returns(new Dictionary<string, CloudFoundryInstance> {
                    { "fake cf name", fakeCfInstance }
                });

            var cfivm = new CfInstanceViewModel(fakeCfInstance, services);

            var fakeOrg = new CloudFoundryOrganization("fake org name", "fake org id", fakeCfInstance);
            var ovm = new OrgViewModel(fakeOrg, services);

            var fakeSpace = new CloudFoundrySpace("fake space name", "fake space id", fakeOrg);
            var svm = new SpaceViewModel(fakeSpace, services);

            cfivm.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                ovm
            };

            ovm.Children = new ObservableCollection<TreeViewItemViewModel>
            {
                svm
            };

            // check for presence of Dummy child (sanity check)
            Assert.AreEqual(1, svm.Children.Count);
            Assert.IsNull(svm.Children[0]);

            mockCloudFoundryService.Setup(mock => mock.GetOrgsForCfInstanceAsync(fakeCfInstance)).ReturnsAsync(new List<CloudFoundryOrganization>
            {
                fakeOrg
            });

            mockCloudFoundryService.Setup(mock => mock.GetSpacesForOrgAsync(fakeOrg)).ReturnsAsync(new List<CloudFoundrySpace>
            {
                fakeSpace
            });

            mockCloudFoundryService.Setup(mock => mock.GetAppsForSpaceAsync(fakeSpace)).ReturnsAsync(new List<CloudFoundryApp>());

            vm.CloudFoundryList = new List<CfInstanceViewModel>
            {
                cfivm
            };

            Exception shouldStayNull = null;
            try
            {
                await vm.RefreshAllCloudConnections(null);
            }
            catch (Exception e)
            {
                shouldStayNull = e;
            }

            Assert.IsNull(shouldStayNull);

            Assert.AreEqual(1, cfivm.Children.Count);
            Assert.AreEqual(ovm, cfivm.Children[0]);

            Assert.AreEqual(1, ovm.Children.Count);
            Assert.AreEqual(svm, ovm.Children[0]);

            Assert.AreEqual(1, svm.Children.Count);
            Assert.IsNull(svm.Children[0]);

            mockCloudFoundryService.VerifyAll();
        }

    }
}
