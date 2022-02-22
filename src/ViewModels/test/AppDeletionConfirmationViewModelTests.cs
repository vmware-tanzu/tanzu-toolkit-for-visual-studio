using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.ViewModels.AppDeletionConfirmation;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class AppDeletionConfirmationViewModelTests : ViewModelTestSupport
    {
        private AppDeletionConfirmationViewModel _sut;
        private List<string> _receivedEvents;
        object _fakeConfirmationWindow = new object();
        private CloudFoundryApp _fakeCfApp = new CloudFoundryApp("fake app name", "fake-app-guid", FakeCfSpace, "fake invalid app state");

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();

            _sut = new AppDeletionConfirmationViewModel(Services) { CfApp = _fakeCfApp };
            _receivedEvents = new List<string>();

            _sut.PropertyChanged += (sender, e) =>
            {
                _receivedEvents.Add(e.PropertyName);
            };
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        [TestCategory("ShowConfirmation")]
        public void ShowConfirmation_RecordsAppValue_AndDisplaysDeletionConfirmationDialog()
        {
            _sut.ShowConfirmation(_fakeCfApp);

            MockDialogService.Verify(ds => ds.ShowDialog(nameof(AppDeletionConfirmationViewModel), null), Times.Once);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        [DataRow(true)]
        [DataRow(false)]
        public async Task DeleteApp_ClearsCfAppValue_AndClosesDialog_WhenDeleteAppAsyncSucceeds(bool deleteRoutes)
        {
            MockCloudFoundryService.Setup(m => m.DeleteAppAsync(_fakeCfApp, true, deleteRoutes, 1)).ReturnsAsync(FakeSuccessDetailedResult);

            Assert.AreEqual(_fakeCfApp, _sut.CfApp);
            _sut.DeleteRoutes = deleteRoutes;

            await _sut.DeleteApp(_fakeConfirmationWindow);

            Assert.IsNull(_sut.CfApp);
            MockDialogService.Verify(ds => ds.CloseDialog(_fakeConfirmationWindow, true), Times.Once);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        [DataRow(true)]
        [DataRow(false)]
        public async Task DeleteApp_LogsError_DisplaysError_ClearsCfAppValue_AndClosesDialog_WhenDeleteAppAsyncFails(bool deleteRoutes)
        {
            MockCloudFoundryService.Setup(m => m.DeleteAppAsync(_fakeCfApp, true, deleteRoutes, 1)).ReturnsAsync(FakeFailureDetailedResult);

            Assert.AreEqual(_fakeCfApp, _sut.CfApp);
            _sut.DeleteRoutes = deleteRoutes;

            await _sut.DeleteApp(_fakeConfirmationWindow);

            Assert.IsNull(_sut.CfApp);
            MockDialogService.Verify(ds => ds.CloseDialog(_fakeConfirmationWindow, true), Times.Once);
            MockLogger.Verify(m => m.Error(It.Is<string>(s => s.Contains(AppDeletionConfirmationViewModel._deleteAppErrorMsg)), It.Is<string>(s => s == _fakeCfApp.AppName), It.Is<string>(s => s == FakeFailureDetailedResult.ToString())), Times.Once);
            MockErrorDialogService.Verify(m => m.DisplayErrorDialog(It.Is<string>(s => s.Contains(AppDeletionConfirmationViewModel._deleteAppErrorMsg) && s.Contains(_fakeCfApp.AppName)), It.Is<string>(s => s.Contains(FakeFailureDetailedResult.Explanation))), Times.Once);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        [DataRow(true)]
        [DataRow(false)]
        public async Task DeleteApp_LogsError_DisplaysError_ClearsCfAppValue_AndClosesDialog_WhenDeleteAppAsyncThrowsException(bool deleteRoutes)
        {
            var fakeExceptionMsg = "something went wrong in DeleteAppAsync ;)";

            MockCloudFoundryService.Setup(m => m.DeleteAppAsync(_fakeCfApp, true, deleteRoutes, 1)).Throws(new Exception(fakeExceptionMsg));

            Assert.AreEqual(_fakeCfApp, _sut.CfApp);
            _sut.DeleteRoutes = deleteRoutes;

            await _sut.DeleteApp(_fakeConfirmationWindow);

            Assert.IsNull(_sut.CfApp);
            MockDialogService.Verify(ds => ds.CloseDialog(_fakeConfirmationWindow, true), Times.Once);
            MockLogger.Verify(m => m.Error(It.Is<string>(s => s.Contains(AppDeletionConfirmationViewModel._deleteAppErrorMsg)), It.Is<string>(s => s == _fakeCfApp.AppName), It.Is<string>(s => s == fakeExceptionMsg)), Times.Once);
            MockErrorDialogService.Verify(m => m.DisplayWarningDialog(It.Is<string>(title => title.Contains(AppDeletionConfirmationViewModel._deleteAppErrorMsg) && title.Contains(_fakeCfApp.AppName)), It.Is<string>(msg => msg.Contains("Something unexpected happened") && msg.Contains(_fakeCfApp.AppName))), Times.Once);
        }
    }
}
