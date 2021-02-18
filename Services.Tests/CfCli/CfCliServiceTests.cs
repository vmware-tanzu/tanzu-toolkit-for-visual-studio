﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Services.CfCli;
using Tanzu.Toolkit.VisualStudio.Services.CmdProcess;
using static Tanzu.Toolkit.VisualStudio.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.VisualStudio.Services.Tests.CfCli
{
    [TestClass()]
    public class CfCliServiceTests : CfCliServiceTestSupport
    {
        private CfCliService _sut;
        private static readonly string _fakeArguments = "fake args";
        private static readonly string _fakePathToCfExe = "this\\is\\a\\fake\\path";
        private static readonly string _fakeStdOut = "some output content";
        private static readonly string _fakeStdErr = "some error content";
        private static readonly string _fakeRealisticTokenOutput = "bearer my.fake.token\n";
        private static readonly CmdResult _fakeSuccessResult = new CmdResult("junk output", "junk error", 0);
        private static readonly CmdResult _fakeFailureResult = new CmdResult("junk output", "junk error", 1);
        private static readonly StdOutDelegate _fakeOutCallback = delegate (string content) { };
        private static readonly StdErrDelegate _fakeErrCallback = delegate (string content) { };
        private static readonly CmdResult _fakeOrgsCmdResult = new CmdResult(_fakeMultiPageOrgsOutput, string.Empty, 0);


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
        public async Task InvokeCfCliAsync_ReturnsTrueResult_WhenProcessSucceeded()
        {
            mockCmdProcessService.Setup(mock => mock.InvokeWindowlessCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>()))
                .ReturnsAsync(_fakeSuccessResult);

            DetailedResult result = await _sut.InvokeCfCliAsync(_fakeArguments);

            Assert.AreEqual(true, result.Succeeded);
        }

        [TestMethod()]
        public async Task InvokeCfCliAsync_ReturnsFalseResult_WhenProcessFails()
        {
            mockCmdProcessService.Setup(mock => mock.InvokeWindowlessCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>()))
                .ReturnsAsync(_fakeFailureResult);

            DetailedResult result = await _sut.InvokeCfCliAsync(_fakeArguments);

            Assert.IsFalse(result.Succeeded);
        }

        [TestMethod()]
        public async Task InvokeCfCliAsync_ReturnsFalseResult_WhenCfExeCouldNotBeFound()
        {
            mockFileLocatorService.SetupGet(mock => mock.FullPathToCfExe).Returns((string)null);

            DetailedResult result = await _sut.InvokeCfCliAsync(_fakeArguments);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains("Unable to locate cf.exe"));
        }

        [TestMethod]
        public void ExecuteCfCliCommand_ReturnsTrueResult_WhenProcessSucceeded()
        {
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {_fakeArguments}";

            mockCmdProcessService.Setup(mock => mock.
              ExecuteWindowlessCommand(expectedCmdStr, null))
                .Returns(_fakeSuccessResult);

            DetailedResult result = _sut.ExecuteCfCliCommand(_fakeArguments);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
        }

        [TestMethod]
        public void ExecuteCfCliCommand_ReturnsFalseResult_WhenProcessFails()
        {
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {_fakeArguments}";

            mockCmdProcessService.Setup(mock => mock.
              ExecuteWindowlessCommand(expectedCmdStr, null))
                .Returns(_fakeFailureResult);

            DetailedResult result = _sut.ExecuteCfCliCommand(_fakeArguments);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains($"Unable to execute `cf {_fakeArguments}`."));
            Assert.AreEqual(_fakeFailureResult, result.CmdDetails);
        }

        [TestMethod]
        public void ExecuteCfCliCommand_ReturnsFalseResult_WhenCfExeCouldNotBeFound()
        {
            mockFileLocatorService.SetupGet(mock => mock.
              FullPathToCfExe)
                .Returns((string)null);

            DetailedResult result = _sut.ExecuteCfCliCommand(_fakeArguments);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(_sut.cfExePathErrorMsg, result.Explanation);
        }

        [TestMethod()]
        public async Task InvokeCfCliAsync_UsesDefaultDir_WhenNotSpecified()
        {
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {_fakeArguments}";
            string expectedWorkingDir = null;

            mockCmdProcessService.Setup(mock => mock.InvokeWindowlessCommandAsync(It.IsAny<string>(), expectedWorkingDir, It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>()))
                .ReturnsAsync(_fakeSuccessResult);

            DetailedResult result = await _sut.InvokeCfCliAsync(_fakeArguments);

            mockCmdProcessService.Verify(mock => mock.InvokeWindowlessCommandAsync(expectedCmdStr, expectedWorkingDir, null, null), Times.Once());
        }

        [TestMethod]
        public void GetOAuthToken_ReturnsNull_WhenProcessExitsWithNonZeroCode()
        {
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetOAuthTokenCmd}";

            mockCmdProcessService.Setup(mock => mock.
              ExecuteWindowlessCommand(expectedCmdStr, null))
                .Returns(new CmdResult(_fakeStdOut, _fakeStdErr, exitCode: 1));

            var token = _sut.GetOAuthToken();

            Assert.IsNull(token);
        }

        [TestMethod]
        public void GetOAuthToken_TrimsPrefix_WhenResultStartsWithBearer()
        {
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetOAuthTokenCmd}";

            mockCmdProcessService.Setup(mock => mock.
              ExecuteWindowlessCommand(expectedCmdStr, null))
                .Returns(new CmdResult(_fakeRealisticTokenOutput, _fakeStdErr, exitCode: 0));

            var token = _sut.GetOAuthToken();
            Assert.IsFalse(token.Contains("bearer"));
        }

        [TestMethod]
        public void GetOAuthToken_RemovesNewlinesFromTokenResult()
        {
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetOAuthTokenCmd}";

            mockCmdProcessService.Setup(mock => mock.
              ExecuteWindowlessCommand(expectedCmdStr, null))
                .Returns(new CmdResult(_fakeRealisticTokenOutput, _fakeStdErr, exitCode: 0));

            var token = _sut.GetOAuthToken();

            Assert.IsFalse(token.Contains("\n"));
        }

        [TestMethod]
        public void TargetApi_ReturnsTrue_WhenCmdExitCodeIsZero()
        {
            var fakeApiAddress = "my.api.addr";
            bool skipSsl = true;
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {CfCliService.V6_TargetApiCmd} {fakeApiAddress} --skip-ssl-validation";

            mockCmdProcessService.Setup(mock => mock.
              ExecuteWindowlessCommand(expectedCmdStr, null))
                .Returns(new CmdResult(_fakeStdOut, _fakeStdErr, 0));

            DetailedResult result = _sut.TargetApi(fakeApiAddress, skipSsl);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.CmdDetails.ExitCode == 0);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdDetails.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdDetails.StdErr);
        }

        [TestMethod]
        public void TargetApi_ReturnsFalse_WhenCmdExitCodeIsNotZero()
        {
            var fakeApiAddress = "my.api.addr";
            bool skipSsl = true;
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {CfCliService.V6_TargetApiCmd} {fakeApiAddress} --skip-ssl-validation";

            mockCmdProcessService.Setup(mock => mock.
              ExecuteWindowlessCommand(expectedCmdStr, null))
                .Returns(new CmdResult(_fakeStdOut, _fakeStdErr, 1));

            DetailedResult result = _sut.TargetApi(fakeApiAddress, skipSsl);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.CmdDetails.ExitCode == 1);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdDetails.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdDetails.StdErr);
        }

        [TestMethod]
        public async Task Authenticate_ReturnsTrue_WhenCmdExitCodeIsZero()
        {
            var fakeUsername = "uname";
            var fakePw = new SecureString();
            var fakeDecodedPw = "";
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {CfCliService.V6_AuthenticateCmd} {fakeUsername} {fakeDecodedPw}";

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedCmdStr, null, null, null))
                .ReturnsAsync(new CmdResult(_fakeStdOut, _fakeStdErr, 0));

            DetailedResult result = await _sut.AuthenticateAsync(fakeUsername, fakePw);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.CmdDetails.ExitCode == 0);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdDetails.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdDetails.StdErr);
        }

        [TestMethod]
        public async Task Authenticate_ReturnsFalse_WhenCmdExitCodeIsNotZero()
        {
            var fakeUsername = "uname";
            var fakePw = new SecureString();
            var fakeDecodedPw = "";
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {CfCliService.V6_AuthenticateCmd} {fakeUsername} {fakeDecodedPw}";

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedCmdStr, null, null, null))
                .ReturnsAsync(new CmdResult(_fakeStdOut, _fakeStdErr, 1));

            DetailedResult result = await _sut.AuthenticateAsync(fakeUsername, fakePw);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.CmdDetails.ExitCode == 1);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdDetails.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdDetails.StdErr);
        }

        [TestMethod]
        public async Task InvokeCfCliAsync_ReturnsTrueDetailedResult_WhenCmdExitsWithZeroCode()
        {
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {_fakeArguments}";

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedCmdStr, null, _fakeOutCallback, _fakeErrCallback))
                .ReturnsAsync(new CmdResult(_fakeStdOut, _fakeStdErr, 0));

            var result = await _sut.InvokeCfCliAsync(_fakeArguments, _fakeOutCallback, _fakeErrCallback);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsTrue(result.CmdDetails.ExitCode == 0);
            Assert.IsTrue(result.CmdDetails.StdOut == _fakeStdOut);
            Assert.IsTrue(result.CmdDetails.StdErr == _fakeStdErr);
        }

        [TestMethod]
        public async Task InvokeCfCliAsync_ReturnsFalseDetailedResult_WhenCmdExitsWithNonZeroCode()
        {
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {_fakeArguments}";

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedCmdStr, null, _fakeOutCallback, _fakeErrCallback))
                .ReturnsAsync(new CmdResult(_fakeStdOut, _fakeStdErr, 1));

            var result = await _sut.InvokeCfCliAsync(_fakeArguments, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains($"Unable to execute `cf {_fakeArguments}`."));
            Assert.IsTrue(result.CmdDetails.ExitCode == 1);
            Assert.IsTrue(result.CmdDetails.StdOut == _fakeStdOut);
            Assert.IsTrue(result.CmdDetails.StdErr == _fakeStdErr);
        }

        [TestMethod]
        public async Task GetOrgsAsync_ReturnsListOfOrgs_WhenCmdSucceeds()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetOrgsCmd} -v";

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                .ReturnsAsync(_fakeOrgsCmdResult);

            var result = await _sut.GetOrgsAsync();

            int numOrgsInFakeResponse = 54;
            Assert.AreEqual(numOrgsInFakeResponse, result.Count);
            Assert.AreEqual(_fakeOrgName1, result[0].entity.name);
            Assert.AreEqual(_fakeOrgGuid1, result[0].metadata.guid);
        }

        [TestMethod]
        public async Task GetOrgsAsync_ReturnsEmptyList_WhenCmdExitsWithNonZeroCode()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetOrgsCmd} -v";
            var fakeFailureCmdResult = new CmdResult(string.Empty, string.Empty, 1);

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                .ReturnsAsync(fakeFailureCmdResult);

            var result = await _sut.GetOrgsAsync();

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task GetOrgsAsync_ReturnsEmptyList_WhenJsonParsingFails()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetOrgsCmd} -v";
            var fakeInvalidJsonOutput = $"REQUEST {CfCliService.V6_GetOrgsRequestPath} asdf RESPONSE asdf";
            var fakeFailureCmdResult = new CmdResult(fakeInvalidJsonOutput, string.Empty, 0);

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                .ReturnsAsync(fakeFailureCmdResult);

            var result = await _sut.GetOrgsAsync();

            Assert.AreEqual(0, result.Count);
        }
    }
}