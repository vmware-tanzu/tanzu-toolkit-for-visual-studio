using Microsoft.VisualStudio.TestTools.UnitTesting;
using TanzuForVS.ViewModels;
using System.Collections.Generic;
using TanzuForVS.Models;
using TanzuForVS.Services;
using Moq;
using System.Threading.Tasks;

namespace TanzuForVS.ViewModelsTests
{
    [TestClass()]
    public class DeploymentDialogViewModelTests : ViewModelTestSupport
    {
        private static CloudFoundryInstance _fakeCfInstance = new CloudFoundryInstance("","","");
        private static CloudFoundryOrganization _fakeOrg = new CloudFoundryOrganization("", "", _fakeCfInstance);
        private CloudFoundrySpace _fakeSpace = new CloudFoundrySpace("", "", _fakeOrg);
        private const string _fakeAppName = "fake app name";
        private const string _fakeProjPath = "this\\is\\a\\fake\\path\\to\\a\\project\\directory";
        private DeploymentDialogViewModel _sut;

        [TestInitialize]
        public void TestInit()
        {
            mockCloudFoundryService.SetupGet(mock => mock.CloudFoundryInstances).Returns(new Dictionary<string, CloudFoundryInstance>());

            _sut = new DeploymentDialogViewModel(services, _fakeProjPath);
        }

        [TestMethod()]
        public void DeploymentDialogViewModel_GetsListOfCfsFromCfService_WhenConstructed()
        {        
            var vm = new DeploymentDialogViewModel(services, _fakeProjPath);

            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod()]
        public async Task DeploymentDialogViewModel_DeployAppStatusIsUpdated_WhenDeployAppAsyncSucceeds()
        {
            _sut.AppName = _fakeAppName;
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            mockCloudFoundryService.Setup(mock => mock.DeployAppAsync(_fakeCfInstance,
                _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath))
                .ReturnsAsync(new DetailedResult(true));

            bool DeploymentStatusPropertyChangedCalled = false;

            _sut.PropertyChanged += (s, args) =>
            {
                if ("DeploymentStatus" == args.PropertyName) DeploymentStatusPropertyChangedCalled = true;
            };

            await _sut.DeployApp(null);

            Assert.IsTrue(DeploymentStatusPropertyChangedCalled);
            Assert.AreEqual("App was successfully deployed!", _sut.DeploymentStatus);
            mockCloudFoundryService.VerifyAll();
        }

        [TestMethod()]
        public async Task DeploymentDialogViewModel_DeployAppStatusIsUpdated_WhenDeployAppAsyncFails()
        {
            _sut.AppName = _fakeAppName;
            _sut.SelectedCf = _fakeCfInstance;
            _sut.SelectedOrg = _fakeOrg;
            _sut.SelectedSpace = _fakeSpace;

            mockCloudFoundryService.Setup(mock => mock.DeployAppAsync(_fakeCfInstance,
               _fakeOrg, _fakeSpace, _fakeAppName, _fakeProjPath))
               .ReturnsAsync(new DetailedResult(false, "it failed"));

            bool DeploymentStatusPropertyChangedCalled = false;

            _sut.PropertyChanged += (s, args) =>
            {
                if ("DeploymentStatus" == args.PropertyName) DeploymentStatusPropertyChangedCalled = true;
            };

            await _sut.DeployApp(null);

            Assert.IsTrue(DeploymentStatusPropertyChangedCalled);
            Assert.AreEqual("it failed", _sut.DeploymentStatus);
            mockCloudFoundryService.VerifyAll();
        }
    }
}