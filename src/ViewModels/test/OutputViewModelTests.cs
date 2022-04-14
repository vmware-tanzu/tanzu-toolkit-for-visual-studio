using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Tanzu.Toolkit.Services;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    [TestClass]
    public class OutputViewModelTests : ViewModelTestSupport
    {
        private OutputViewModel _sut;
        private FakeOutputView _fakeOutputView;
        private List<string> _receivedEvents;
        private Process _fakeLogStreamProcess;
        private DetailedResult<string> _fakeRecentLogsResult;
        private DetailedResult<Process> _fakeLogsStreamResult;
        private FakeCfInstanceViewModel _fakeCfInstanceViewModel;
        private ITasExplorerViewModel _fakeTasExplorerViewModel;

        [TestInitialize]
        public void TestInit()
        {
            RenewMockServices();
            _receivedEvents = new List<string>();
            _fakeLogStreamProcess = new Process();
            _fakeRecentLogsResult = new DetailedResult<string>
            {
                Succeeded = true,
                Content = "pretend this is a recent log statement",
            };
            _fakeLogsStreamResult = new DetailedResult<Process>
            {
                Succeeded = true,
                Content = _fakeLogStreamProcess,
            };

            _fakeCfInstanceViewModel = new FakeCfInstanceViewModel(_fakeCfInstance, Services);
            _fakeTasExplorerViewModel = MockTasExplorerViewModel.Object;

            MockTasExplorerViewModel.Setup(m => m.TasConnection).Returns(_fakeCfInstanceViewModel);
            MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(_fakeCfApp)).ReturnsAsync(_fakeRecentLogsResult);
            MockCloudFoundryService.Setup(m => m.StreamAppLogs(_fakeCfApp, _sut.AppendLine, _sut.AppendLine)).Returns(_fakeLogsStreamResult);

            _sut = new OutputViewModel(_fakeTasExplorerViewModel, Services);

            _fakeOutputView = new FakeOutputView
            {
                ViewModel = _sut,
            };

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
        [TestCategory("BeginStreamingAppLogsForAppAsync")]
        public async Task BeginStreamingAppLogsForAppAsync_DisplaysOutputView()
        {
            Assert.IsFalse(_fakeOutputView.ShowMethodWasCalled);
            await _sut.BeginStreamingAppLogsForAppAsync(_fakeCfApp, _fakeOutputView);
            Assert.IsTrue(_fakeOutputView.ShowMethodWasCalled);
        }

        [TestMethod]
        [TestCategory("BeginStreamingAppLogsForAppAsync")]
        public async Task BeginStreamingAppLogsForAppAsync_MarksOutputIsAppLogsAsTrue()
        {
            Assert.IsFalse(_sut.OutputIsAppLogs);
            await _sut.BeginStreamingAppLogsForAppAsync(_fakeCfApp, _fakeOutputView);
            Assert.IsTrue(_sut.OutputIsAppLogs);
        }

        [TestMethod]
        [TestCategory("BeginStreamingAppLogsForAppAsync")]
        public async Task BeginStreamingAppLogsForAppAsync_PrintsRecentLogs_BeforeStartingStream()
        {
            var logsStreamException = new Exception("just breaking execution to make sure recent logs appear before logs stream");

            MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(_fakeCfApp)).ReturnsAsync(_fakeRecentLogsResult);
            MockCloudFoundryService.Setup(m => m.StreamAppLogs(_fakeCfApp, _sut.AppendLine, _sut.AppendLine)).Throws(logsStreamException);

            Assert.IsNull(_sut.OutputContent);

            try
            {
                await _sut.BeginStreamingAppLogsForAppAsync(_fakeCfApp, _fakeOutputView);
            }
            catch (Exception ex)
            {
                Assert.AreEqual(logsStreamException, ex);
            }
            finally
            {
                Assert.IsTrue(_sut.OutputContent.Contains(_fakeRecentLogsResult.Content));
                Assert.IsNull(_sut.ActiveProcess); // ensure logs stream didn't succeed
            }
        }

        [TestMethod]
        [TestCategory("BeginStreamingAppLogsForAppAsync")]
        public async Task BeginStreamingAppLogsForAppAsync_PrintsFailureMessage_AndLogsError_WhenRecentLogsRequestFails()
        {
            var expectedFailureMsg = "\n*** Unable to fetch recent logs, attempting to start live log stream... ***\n";
            _fakeRecentLogsResult.Succeeded = false;
            _fakeRecentLogsResult.Explanation = ":(";
            MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(_fakeCfApp)).ReturnsAsync(_fakeRecentLogsResult);

            Assert.IsNull(_sut.OutputContent);

            await _sut.BeginStreamingAppLogsForAppAsync(_fakeCfApp, _fakeOutputView);

            Assert.IsTrue(_sut.OutputContent.Contains(expectedFailureMsg));
            MockLogger.Verify(m => m.Error(It.Is<string>(s => s.Contains(_fakeCfApp.AppName) && s.Contains(_fakeRecentLogsResult.Explanation))));
        }

        [TestMethod]
        [TestCategory("BeginStreamingAppLogsForAppAsync")]
        public async Task BeginStreamingAppLogsForAppAsync_SetsAuthRequiredToTrue_WhenRecentLogsRequestFailsWithInvalidRefreshToken()
        {
            _fakeRecentLogsResult.Succeeded = false;
            _fakeRecentLogsResult.FailureType = FailureType.InvalidRefreshToken;
            MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(_fakeCfApp)).ReturnsAsync(_fakeRecentLogsResult);

            await _sut.BeginStreamingAppLogsForAppAsync(_fakeCfApp, _fakeOutputView);

            MockTasExplorerViewModel.VerifySet(m => m.AuthenticationRequired = true);
        }

        [TestMethod]
        [TestCategory("BeginStreamingAppLogsForAppAsync")]
        public async Task BeginStreamingAppLogsForAppAsync_SetsActiveProcess_WhenLogsStreamSucceeds()
        {
            MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(_fakeCfApp)).ReturnsAsync(_fakeRecentLogsResult);
            MockCloudFoundryService.Setup(m => m.StreamAppLogs(_fakeCfApp, _sut.AppendLine, _sut.AppendLine)).Returns(_fakeLogsStreamResult);

            Assert.IsNull(_sut.ActiveProcess);

            await _sut.BeginStreamingAppLogsForAppAsync(_fakeCfApp, _fakeOutputView);

            Assert.AreEqual(_fakeLogsStreamResult.Content, _sut.ActiveProcess);
        }

        [TestMethod]
        [TestCategory("BeginStreamingAppLogsForAppAsync")]
        public async Task BeginStreamingAppLogsForAppAsync_SetsAuthRequiredToTrue_WhenLogsStreamFailsWithInvalidRefreshToken()
        {
            _fakeLogsStreamResult.Succeeded = false;
            _fakeLogsStreamResult.FailureType = FailureType.InvalidRefreshToken;

            MockCloudFoundryService.Setup(m => m.GetRecentLogsAsync(_fakeCfApp)).ReturnsAsync(_fakeRecentLogsResult);
            MockCloudFoundryService.Setup(m => m.StreamAppLogs(_fakeCfApp, _sut.AppendLine, _sut.AppendLine)).Returns(_fakeLogsStreamResult);

            await _sut.BeginStreamingAppLogsForAppAsync(_fakeCfApp, _fakeOutputView);

            MockTasExplorerViewModel.VerifySet(m => m.AuthenticationRequired = true);
        }
    }
}
