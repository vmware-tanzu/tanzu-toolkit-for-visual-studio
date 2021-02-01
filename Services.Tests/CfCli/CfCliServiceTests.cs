using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.IO;
using System.Threading.Tasks;
using TanzuForVS.Services.CfCli;
using static TanzuForVS.Services.CfCli.StdOutHandler;

namespace TanzuForVS.Services.Tests
{
    [TestClass()]
    public class CfCliServiceTests : ServicesTestSupport
    {
        private CfCliService _sut;
        private string _fakeArguments = "fake args";
        private string _fakePathToCfExe = "this\\is\\a\\fake\\path";

        [TestInitialize]
        public void TestInit()
        {
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
            mockFileLocatorService.SetupGet(mock => mock.FullPathToCfExe).Returns(_fakePathToCfExe);

            mockCmdProcessService.Setup(mock => mock.ExecuteWindowlessCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StdOutDelegate>()))
                .ReturnsAsync(true);

            DetailedResult result = await _sut.ExecuteCfCliCommandAsync(_fakeArguments, stdOutHandler: null);

            Assert.AreEqual(true, result.Succeeded);
        }

        [TestMethod()]
        public async Task ExecuteCfCliCommandAsync_ReturnsFalseResult_WhenProcessFails()
        {
            mockFileLocatorService.SetupGet(mock => mock.FullPathToCfExe).Returns(_fakePathToCfExe);

            mockCmdProcessService.Setup(mock => mock.ExecuteWindowlessCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StdOutDelegate>()))
                .ReturnsAsync(false);

            DetailedResult result = await _sut.ExecuteCfCliCommandAsync(_fakeArguments, stdOutHandler: null);

            Assert.IsFalse(result.Succeeded);
        }

        [TestMethod()]
        public async Task ExecuteCfCliCommandAsync_ReturnsFalseResult_WhenCfExeCouldNotBeFound()
        {
            mockFileLocatorService.SetupGet(mock => mock.FullPathToCfExe).Returns((string)null);

            DetailedResult result = await _sut.ExecuteCfCliCommandAsync(_fakeArguments, stdOutHandler: null);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains("Unable to locate cf.exe"));
        }

        [TestMethod()]
        public async Task ExecuteCfCliCommandAsync_UsesDefaultDir_WhenNotSpecified()
        {
            mockFileLocatorService.SetupGet(mock => mock.FullPathToCfExe).Returns(_fakePathToCfExe);

            mockCmdProcessService.Setup(mock => mock.ExecuteWindowlessCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StdOutDelegate>()))
                .ReturnsAsync(true);

            DetailedResult result = await _sut.ExecuteCfCliCommandAsync(_fakeArguments, stdOutHandler: null);

            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {_fakeArguments}";
            string expectedWorkingDir = null;
            mockCmdProcessService.Verify(mock => mock.ExecuteWindowlessCommandAsync(expectedCmdStr, expectedWorkingDir, null), Times.Once());
        }
    }
}