using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Services.CfCli;
using Tanzu.Toolkit.VisualStudio.Services.CmdProcess;
using static Tanzu.Toolkit.VisualStudio.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.VisualStudio.Services.Tests.CfCli
{
    [TestClass()]
    public class CfCliServiceTests : ServicesTestSupport
    {
        private CfCliService _sut;
        private readonly string _fakeArguments = "fake args";
        private readonly string _fakePathToCfExe = "this\\is\\a\\fake\\path";
        private readonly string _fakeStdOut = "some output content";
        private readonly string _fakeStdErr = "some error content";
        private readonly string _fakeRealisticTokenOutput = "bearer my.fake.token\n";

        [TestInitialize]
        public void TestInit()
        {
            mockFileLocatorService.SetupGet(mock => mock.FullPathToCfExe).Returns(_fakePathToCfExe);
            _sut = new CfCliService(services);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            mockFileLocatorService.VerifyAll();
            mockCmdProcessService.VerifyAll();
        }

        [TestMethod()]
        public async Task ExecuteCfCliCommandAsync_ReturnsTrueResult_WhenProcessSucceeded()
        {
            mockCmdProcessService.Setup(mock => mock.InvokeWindowlessCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StdOutDelegate>()))
                .ReturnsAsync(true);

            DetailedResult result = await _sut.InvokeCfCliAsync(_fakeArguments, stdOutHandler: null);

            Assert.AreEqual(true, result.Succeeded);
        }

        [TestMethod()]
        public async Task ExecuteCfCliCommandAsync_ReturnsFalseResult_WhenProcessFails()
        {
            mockCmdProcessService.Setup(mock => mock.InvokeWindowlessCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StdOutDelegate>()))
                .ReturnsAsync(false);

            DetailedResult result = await _sut.InvokeCfCliAsync(_fakeArguments, stdOutHandler: null);

            Assert.IsFalse(result.Succeeded);
        }

        [TestMethod()]
        public async Task ExecuteCfCliCommandAsync_ReturnsFalseResult_WhenCfExeCouldNotBeFound()
        {
            mockFileLocatorService.SetupGet(mock => mock.FullPathToCfExe).Returns((string)null);

            DetailedResult result = await _sut.InvokeCfCliAsync(_fakeArguments, stdOutHandler: null);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains("Unable to locate cf.exe"));
        }

        [TestMethod()]
        public async Task ExecuteCfCliCommandAsync_UsesDefaultDir_WhenNotSpecified()
        {
            mockCmdProcessService.Setup(mock => mock.InvokeWindowlessCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StdOutDelegate>()))
                .ReturnsAsync(true);

            DetailedResult result = await _sut.InvokeCfCliAsync(_fakeArguments, stdOutHandler: null);

            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {_fakeArguments}";
            string expectedWorkingDir = null;
            mockCmdProcessService.Verify(mock => mock.InvokeWindowlessCommandAsync(expectedCmdStr, expectedWorkingDir, null), Times.Once());
        }

        [TestMethod]
        public void GetOAuthToken_ReturnsNull_WhenProcessExitsWithNonZeroCode()
        {
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetOAuthTokenCmd}";

            mockCmdProcessService.Setup(mock => mock.
              ExecuteWindowlessCommand(expectedCmdStr, null))
                .Returns(new CmdOutput(_fakeStdOut, _fakeStdErr, exitCode: 1));

            var token = _sut.GetOAuthToken();

            Assert.IsNull(token);
        }

        [TestMethod]
        public void GetOAuthToken_TrimsPrefix_WhenResultStartsWithBearer()
        {
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetOAuthTokenCmd}";

            mockCmdProcessService.Setup(mock => mock.
              ExecuteWindowlessCommand(expectedCmdStr, null))
                .Returns(new CmdOutput(_fakeRealisticTokenOutput, _fakeStdErr, exitCode: 0));

            var token = _sut.GetOAuthToken();
            Assert.IsFalse(token.Contains("bearer"));
        }
        
        [TestMethod]
        public void GetOAuthToken_RemovesNewlinesFromTokenResult()
        {
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetOAuthTokenCmd}";

            mockCmdProcessService.Setup(mock => mock.
              ExecuteWindowlessCommand(expectedCmdStr, null))
                .Returns(new CmdOutput(_fakeRealisticTokenOutput, _fakeStdErr, exitCode: 0));
            
            var token = _sut.GetOAuthToken();

            Assert.IsFalse(token.Contains("\n"));
        }

    }
}