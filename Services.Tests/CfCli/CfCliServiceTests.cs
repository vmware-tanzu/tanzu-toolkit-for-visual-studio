using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.VisualStudio.Services.CfCli;
using Tanzu.Toolkit.VisualStudio.Services.CfCli.Models.Apps;
using Tanzu.Toolkit.VisualStudio.Services.CfCli.Models.Orgs;
using Tanzu.Toolkit.VisualStudio.Services.CfCli.Models.Spaces;
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
        private static readonly CmdResult _fakeNoOrgsCmdResult = new CmdResult(_fakeNoOrgsOutput, string.Empty, 0);
        private static readonly CmdResult _fakeSpacesCmdResult = new CmdResult(_fakeMultiPageSpacesOutput, string.Empty, 0);
        private static readonly CmdResult _fakeNoSpacesCmdResult = new CmdResult(_fakeNoSpacesOutput, string.Empty, 0);
        private static readonly CmdResult _fakeAppsCmdResult = new CmdResult(_fakeManyAppsOutput, string.Empty, 0);
        private static readonly CmdResult _fakeNoAppsCmdResult = new CmdResult(_fakeNoAppsOutput, string.Empty, 0);


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


        [TestMethod]
        [TestCategory("InvokeCfCliAsync")]
        public async Task InvokeCfCliAsync_ReturnsSuccessfulResult_WhenCmdProcessExitsWithZeroCode()
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
        [TestCategory("InvokeCfCliAsync")]
        public async Task InvokeCfCliAsync_ReturnsFailedResult_WhenCmdProcessExitsWithNonZeroCode()
        {
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {_fakeArguments}";

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedCmdStr, null, _fakeOutCallback, _fakeErrCallback))
                .ReturnsAsync(new CmdResult(_fakeStdOut, _fakeStdErr, 1));

            var result = await _sut.InvokeCfCliAsync(_fakeArguments, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(_fakeStdErr));
            Assert.IsTrue(result.CmdDetails.ExitCode == 1);
            Assert.IsTrue(result.CmdDetails.StdOut == _fakeStdOut);
            Assert.IsTrue(result.CmdDetails.StdErr == _fakeStdErr);
        }

        [TestMethod]
        [TestCategory("InvokeCfCliAsync")]
        public async Task InvokeCfCliAsync_ReturnsStdOut_WhenProcessFailsWithoutStdErr_AndStdOutContainsFAILEDSubstring()
        {
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {_fakeArguments}";
            const string mockStdOutContainingFailedSubstring = "FAILED this is a mock response";

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedCmdStr, null, _fakeOutCallback, _fakeErrCallback))
                .ReturnsAsync(new CmdResult(mockStdOutContainingFailedSubstring, string.Empty, 1));

            var result = await _sut.InvokeCfCliAsync(_fakeArguments, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(mockStdOutContainingFailedSubstring));
            Assert.IsTrue(result.CmdDetails.ExitCode == 1);
            Assert.IsTrue(result.CmdDetails.StdOut == mockStdOutContainingFailedSubstring);
            Assert.IsTrue(result.CmdDetails.StdErr == string.Empty);
        }

        [TestMethod]
        [TestCategory("InvokeCfCliAsync")]
        public async Task InvokeCfCliAsync_ReturnsGenericExplanation_WhenProcessFailsWithoutStdErr()
        {
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {_fakeArguments}";

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedCmdStr, null, _fakeOutCallback, _fakeErrCallback))
                .ReturnsAsync(new CmdResult(_fakeStdOut, string.Empty, 1));

            var result = await _sut.InvokeCfCliAsync(_fakeArguments, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains($"Unable to execute `cf {_fakeArguments}`."));
            Assert.IsTrue(result.CmdDetails.ExitCode == 1);
            Assert.IsTrue(result.CmdDetails.StdOut == _fakeStdOut);
            Assert.IsTrue(result.CmdDetails.StdErr == string.Empty);
        }

        [TestMethod]
        [TestCategory("InvokeCfCliAsync")]
        public async Task InvokeCfCliAsync_ReturnsFalseResult_WhenCfExeCouldNotBeFound()
        {
            mockFileLocatorService.SetupGet(mock => mock.FullPathToCfExe).Returns((string)null);

            DetailedResult result = await _sut.InvokeCfCliAsync(_fakeArguments);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains("Unable to locate cf.exe"));
        }

        [TestMethod]
        [TestCategory("InvokeCfCliAsync")]
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
        [TestCategory("RunCfCommandAsync")]
        public async Task RunCfCommandAsync_ReturnsSuccessfulResult_WhenCmdProcessExitsWithZeroCode()
        {
            mockCmdProcessService.Setup(mock => mock.
              RunCommand(_fakePathToCfExe, _fakeArguments, null, _fakeOutCallback, _fakeErrCallback))
                .Returns(new CmdResult(_fakeStdOut, _fakeStdErr, 0));

            var result = await _sut.RunCfCommandAsync(_fakeArguments, _fakeOutCallback, _fakeErrCallback);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsTrue(result.CmdDetails.ExitCode == 0);
            Assert.IsTrue(result.CmdDetails.StdOut == _fakeStdOut);
            Assert.IsTrue(result.CmdDetails.StdErr == _fakeStdErr);
        }

        [TestMethod]
        [TestCategory("RunCfCommandAsync")]
        public async Task RunCfCommandAsync_ReturnsFailedResult_WhenCmdProcessExitsWithNonZeroCode()
        {
            mockCmdProcessService.Setup(mock => mock.
              RunCommand(_fakePathToCfExe, _fakeArguments, null, _fakeOutCallback, _fakeErrCallback))
                .Returns(new CmdResult(_fakeStdOut, _fakeStdErr, 1));

            var result = await _sut.RunCfCommandAsync(_fakeArguments, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(_fakeStdErr));
            Assert.IsTrue(result.CmdDetails.ExitCode == 1);
            Assert.IsTrue(result.CmdDetails.StdOut == _fakeStdOut);
            Assert.IsTrue(result.CmdDetails.StdErr == _fakeStdErr);
        }

        [TestMethod]
        [TestCategory("RunCfCommandAsync")]
        public async Task RunCfCommandAsync_ReturnsStdOut_WhenProcessFailsWithoutStdErr_AndStdOutContainsFAILEDSubstring()
        {
            const string mockStdOutContainingFailedSubstring = "FAILED this is a mock response";

            mockCmdProcessService.Setup(mock => mock.
              RunCommand(_fakePathToCfExe, _fakeArguments, null, _fakeOutCallback, _fakeErrCallback))
                .Returns(new CmdResult(mockStdOutContainingFailedSubstring, string.Empty, 1));

            var result = await _sut.RunCfCommandAsync(_fakeArguments, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(mockStdOutContainingFailedSubstring));
            Assert.IsTrue(result.CmdDetails.ExitCode == 1);
            Assert.IsTrue(result.CmdDetails.StdOut == mockStdOutContainingFailedSubstring);
            Assert.IsTrue(result.CmdDetails.StdErr == string.Empty);
        }

        [TestMethod]
        [TestCategory("RunCfCommandAsync")]
        public async Task RunCfCommandAsync_ReturnsGenericExplanation_WhenProcessFailsWithoutStdErr()
        {
            mockCmdProcessService.Setup(mock => mock.
              RunCommand(_fakePathToCfExe, _fakeArguments, null, _fakeOutCallback, _fakeErrCallback))
                .Returns(new CmdResult(_fakeStdOut, string.Empty, 1));

            var result = await _sut.RunCfCommandAsync(_fakeArguments, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains($"Unable to execute `cf {_fakeArguments}`."));
            Assert.IsTrue(result.CmdDetails.ExitCode == 1);
            Assert.IsTrue(result.CmdDetails.StdOut == _fakeStdOut);
            Assert.IsTrue(result.CmdDetails.StdErr == string.Empty);
        }

        [TestMethod]
        [TestCategory("RunCfCommandAsync")]
        public async Task RunCfCommandAsync_ReturnsFalseResult_WhenCfExeCouldNotBeFound()
        {
            mockFileLocatorService.SetupGet(mock => mock.FullPathToCfExe).Returns((string)null);

            DetailedResult result = await _sut.RunCfCommandAsync(_fakeArguments);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains("Unable to locate cf.exe"));
        }

        [TestMethod]
        [TestCategory("RunCfCommandAsync")]
        public async Task RunCfCommandAsync_UsesDefaultDir_WhenNotSpecified()
        {
            string expectedWorkingDir = null;

            mockCmdProcessService.Setup(mock => mock.
                RunCommand(_fakePathToCfExe, _fakeArguments, expectedWorkingDir, It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>()))
                    .Returns(_fakeSuccessResult);

            DetailedResult result = await _sut.RunCfCommandAsync(_fakeArguments);

            mockCmdProcessService.Verify(mock => mock.RunCommand(_fakePathToCfExe, _fakeArguments, expectedWorkingDir, null, null), Times.Once());
        }



        [TestMethod]
        [TestCategory("ExecuteCfCliCommand")]
        public void ExecuteCfCliCommand_ReturnsTrueResult_WhenProcessExitCodeIsZero()
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
        [TestCategory("ExecuteCfCliCommand")]
        public void ExecuteCfCliCommand_ReturnsFalseResult_WhenProcessExitCodeIsNotZero()
        {
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {_fakeArguments}";

            mockCmdProcessService.Setup(mock => mock.
              ExecuteWindowlessCommand(expectedCmdStr, null))
                .Returns(_fakeFailureResult);

            DetailedResult result = _sut.ExecuteCfCliCommand(_fakeArguments);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(_fakeFailureResult.StdErr));
            Assert.AreEqual(_fakeFailureResult, result.CmdDetails);
            Assert.IsTrue(result.CmdDetails.StdErr == _fakeFailureResult.StdErr);
        }

        [TestMethod]
        [TestCategory("ExecuteCfCliCommand")]
        public void ExecuteCfCliCommand_ReturnsStdOut_WhenProcessFailsWithoutStdErr_AndStdOutContainsFAILEDSubstring()
        {
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {_fakeArguments}";

            var fakeFailedResult = new CmdResult("FAILED this is a mock response", string.Empty, 1);

            mockCmdProcessService.Setup(mock => mock.
              ExecuteWindowlessCommand(expectedCmdStr, null))
                .Returns(fakeFailedResult);

            DetailedResult result = _sut.ExecuteCfCliCommand(_fakeArguments);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(fakeFailedResult.StdOut));
            Assert.AreEqual(fakeFailedResult, result.CmdDetails);
            Assert.IsTrue(result.CmdDetails.StdErr == string.Empty);
        }

        [TestMethod]
        [TestCategory("ExecuteCfCliCommand")]
        public void ExecuteCfCliCommand_ReturnsGenericExplanation_WhenProcessFailsWithoutStdErr()
        {
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {_fakeArguments}";

            var fakeFailedResult = new CmdResult("junk output", string.Empty, 1);
            mockCmdProcessService.Setup(mock => mock.
              ExecuteWindowlessCommand(expectedCmdStr, null))
                .Returns(fakeFailedResult);

            DetailedResult result = _sut.ExecuteCfCliCommand(_fakeArguments);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains($"Unable to execute `cf {_fakeArguments}`."));
            Assert.AreEqual(fakeFailedResult, result.CmdDetails);
            Assert.IsTrue(result.CmdDetails.StdErr == string.Empty);
        }

        [TestMethod]
        [TestCategory("ExecuteCfCliCommand")]
        public void ExecuteCfCliCommand_ReturnsFalseResult_WhenCfExeCouldNotBeFound()
        {
            mockFileLocatorService.SetupGet(mock => mock.
              FullPathToCfExe)
                .Returns((string)null);

            DetailedResult result = _sut.ExecuteCfCliCommand(_fakeArguments);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(_sut._cfExePathErrorMsg, result.Explanation);
        }



        [TestMethod]
        [TestCategory("GetOAuthToken")]
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
        [TestCategory("GetOAuthToken")]
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
        [TestCategory("GetOAuthToken")]
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
        [TestCategory("TargetApi")]
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
        [TestCategory("TargetApi")]
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
        [TestCategory("Authenticate")]
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
        [TestCategory("Authenticate")]
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
        [TestCategory("GetOrgsAsync")]
        public async Task GetOrgsAsync_ReturnsSuccessfulResult_WhenCmdSucceeds()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetOrgsCmd} -v";

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                .ReturnsAsync(_fakeOrgsCmdResult);

            DetailedResult<List<Org>> result = await _sut.GetOrgsAsync();

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeOrgsCmdResult, result.CmdDetails);

            Assert.AreEqual(numOrgsInFakeResponse, result.Content.Count);
            Assert.AreEqual(_fakeOrgName1, result.Content[0].entity.name);
            Assert.AreEqual(_fakeOrgGuid1, result.Content[0].metadata.guid);
        }

        [TestMethod]
        [TestCategory("GetOrgsAsync")]
        public async Task GetOrgsAsync_ReturnsFailedResult_WhenCmdResultReportsFailure()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetOrgsCmd} -v";

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                .ReturnsAsync(fakeFailureCmdResult);

            DetailedResult<List<Org>> result = await _sut.GetOrgsAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(_sut._requestErrorMsg, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetOrgsAsync")]
        public async Task GetOrgsAsync_ReturnsFailedResult_WhenJsonParsingFails()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetOrgsCmd} -v";
            var fakeInvalidJsonOutput = $"REQUEST {CfCliService.V6_GetOrgsRequestPath} asdf RESPONSE asdf";
            var fakeFailureCmdResult = new CmdResult(fakeInvalidJsonOutput, string.Empty, 0);

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                .ReturnsAsync(fakeFailureCmdResult);

            DetailedResult<List<Org>> result = await _sut.GetOrgsAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(_sut._jsonParsingErrorMsg, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetOrgsAsync")]
        public async Task GetOrgsAsync_ReturnsFailedResult_For401UnauthorizedResponse()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetOrgsCmd} -v";
            var fakeFailureCmdResult = new CmdResult(_fakeOrgs401Output, string.Empty, 0);

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                .ReturnsAsync(fakeFailureCmdResult);

            DetailedResult<List<Org>> result = await _sut.GetOrgsAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(_sut._jsonParsingErrorMsg, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetOrgsAsync")]
        public async Task GetOrgsAsync_ReturnsSuccessfulResult_WhenResponseContainsNoOrgsFound()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetOrgsCmd} -v";

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                .ReturnsAsync(_fakeNoOrgsCmdResult);

            var result = await _sut.GetOrgsAsync();

            Assert.IsTrue(result.Succeeded);
            CollectionAssert.AreEqual(new List<Org>(), result.Content);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeNoOrgsCmdResult, result.CmdDetails);
        }



        [TestMethod]
        [TestCategory("GetSpacesAsync")]
        public async Task GetSpacesAsync_ReturnsSuccessfulResult_WhenCmdSucceeds()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetSpacesCmd} -v";
            
            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                .ReturnsAsync(_fakeSpacesCmdResult);

            DetailedResult<List<Space>> result = await _sut.GetSpacesAsync();

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeSpacesCmdResult, result.CmdDetails);

            Assert.AreEqual(numOrgsInFakeResponse, result.Content.Count);
            Assert.AreEqual(_fakeSpaceName1, result.Content[0].entity.name);
            Assert.AreEqual(_fakeSpaceGuid1, result.Content[0].metadata.guid);
        }

        [TestMethod]
        [TestCategory("GetSpacesAsync")]
        public async Task GetSpacesAsync_ReturnsFailedResult_WhenCmdResultReportsFailure()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetSpacesCmd} -v";

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                .ReturnsAsync(fakeFailureCmdResult);

            DetailedResult<List<Space>> result = await _sut.GetSpacesAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(_sut._requestErrorMsg, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetSpacesAsync")]
        public async Task GetSpacesAsync_ReturnsFailedResult_WhenJsonParsingFails()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetSpacesCmd} -v";
            var fakeInvalidJsonOutput = $"REQUEST {CfCliService.V6_GetSpacesRequestPath} asdf RESPONSE asdf";
            var fakeFailureCmdResult = new CmdResult(fakeInvalidJsonOutput, string.Empty, 0);

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                .ReturnsAsync(fakeFailureCmdResult);

            DetailedResult<List<Space>> result = await _sut.GetSpacesAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(_sut._jsonParsingErrorMsg, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetSpacesAsync")]
        public async Task GetSpacesAsync_ReturnsFailedResult_WhenResponseContainsNoSpacesFound()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetSpacesCmd} -v";

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                .ReturnsAsync(_fakeNoSpacesCmdResult);

            var result = await _sut.GetSpacesAsync();

            Assert.IsTrue(result.Succeeded);
            CollectionAssert.AreEqual(new List<Space>(), result.Content);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeNoSpacesCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetSpacesAsync")]
        public async Task GetSpacesAsync_ReturnsFailedResult_For401UnauthorizedResponse()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetSpacesCmd} -v";
            var fakeFailureCmdResult = new CmdResult(_fakeSpaces401Output, string.Empty, 0);

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                .ReturnsAsync(fakeFailureCmdResult);

            DetailedResult<List<Space>> result = await _sut.GetSpacesAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(_sut._jsonParsingErrorMsg, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }



        [TestMethod]
        [TestCategory("GetAppsAsync")]
        public async Task GetAppsAsync_ReturnsSuccessfulResult_WhenCmdSucceeds()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetAppsCmd} -v";
            int numAppsInFakeResponse = 53;

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                .ReturnsAsync(_fakeAppsCmdResult);

            var result = await _sut.GetAppsAsync();

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeAppsCmdResult, result.CmdDetails);

            Assert.AreEqual(typeof(List<App>), result.Content.GetType());
            Assert.AreEqual(numAppsInFakeResponse, result.Content.Count);
            Assert.AreEqual(_fakeAppName1, result.Content[0].name);
            Assert.AreEqual(_fakeAppGuid1, result.Content[0].guid);
        }

        [TestMethod]
        [TestCategory("GetAppsAsync")]
        public async Task GetAppsAsync_ReturnsFailedResult_WhenCmdResultReportsFailure()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetAppsCmd} -v";
            var fakeFailureCmdResult = new CmdResult(string.Empty, string.Empty, 1);

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                .ReturnsAsync(fakeFailureCmdResult);

            var result = await _sut.GetAppsAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(_sut._requestErrorMsg, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetAppsAsync")]
        public async Task GetAppsAsync_ReturnsFailedResult_WhenJsonParsingFails()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetAppsCmd} -v";
            var fakeInvalidJsonOutput = $"REQUEST {CfCliService.V6_GetAppsRequestPath} asdf RESPONSE asdf";
            var fakeFailureCmdResult = new CmdResult(fakeInvalidJsonOutput, string.Empty, 0);

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                .ReturnsAsync(fakeFailureCmdResult);

            var result = await _sut.GetAppsAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(_sut._jsonParsingErrorMsg, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetAppsAsync")]
        public async Task GetAppsAsync_ReturnsFailedResult_WhenResponseContainsNoAppsFound()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetAppsCmd} -v";

            mockCmdProcessService.Setup(mock => mock.
                InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                    .ReturnsAsync(_fakeNoAppsCmdResult);

            var result = await _sut.GetAppsAsync();

            Assert.IsTrue(result.Succeeded);
            CollectionAssert.AreEqual(new List<App>(), result.Content);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeNoAppsCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetAppsAsync")]
        public async Task GetAppsAsync_ReturnsFailedResult_For401UnauthorizedResponse()
        {
            string expectedArgs = $"\"{_fakePathToCfExe}\" {CfCliService.V6_GetAppsCmd} -v";
            var fakeFailureCmdResult = new CmdResult(_fakeApps401Output, string.Empty, 0);

            mockCmdProcessService.Setup(mock => mock.
              InvokeWindowlessCommandAsync(expectedArgs, null, null, null))
                .ReturnsAsync(fakeFailureCmdResult);

            DetailedResult<List<App>> result = await _sut.GetAppsAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(_sut._jsonParsingErrorMsg, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }




        [TestMethod]
        [TestCategory("TargetOrg")]
        public void TargetOrg_ReturnsTrue_WhenCmdExitCodeIsZero()
        {
            var fakeOrgName = "fake-org";
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {CfCliService.V6_TargetOrgCmd} {fakeOrgName}";
            CmdResult fakeSuccessResult = new CmdResult(_fakeStdOut, _fakeStdErr, 0);

            mockCmdProcessService.Setup(mock => mock.
              ExecuteWindowlessCommand(expectedCmdStr, null))
                .Returns(fakeSuccessResult);

            DetailedResult result = _sut.TargetOrg(fakeOrgName);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.CmdDetails.ExitCode == 0);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdDetails.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdDetails.StdErr);
        }

        [TestMethod]
        [TestCategory("TargetOrg")]
        public void TargetOrg_ReturnsFalse_WhenCmdExitCodeIsNotZero()
        {
            var fakeOrgName = "fake-org";
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {CfCliService.V6_TargetOrgCmd} {fakeOrgName}";
            CmdResult fakeFailureResult = new CmdResult(_fakeStdOut, _fakeStdErr, 1);

            mockCmdProcessService.Setup(mock => mock.
              ExecuteWindowlessCommand(expectedCmdStr, null))
                .Returns(fakeFailureResult);

            DetailedResult result = _sut.TargetOrg(fakeOrgName);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.CmdDetails.ExitCode == 1);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdDetails.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdDetails.StdErr);
        }



        [TestMethod]
        [TestCategory("TargetSpace")]
        public void TargetSpace_ReturnsTrueResult_WhenCmdExitCodeIsZero()
        {
            var fakeSpaceName = "fake-space";
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {CfCliService.V6_TargetSpaceCmd} {fakeSpaceName}";
            CmdResult fakeSuccessResult = new CmdResult(_fakeStdOut, _fakeStdErr, 0);

            mockCmdProcessService.Setup(mock => mock.
              ExecuteWindowlessCommand(expectedCmdStr, null))
                .Returns(fakeSuccessResult);

            DetailedResult result = _sut.TargetSpace(fakeSpaceName);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.CmdDetails.ExitCode == 0);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdDetails.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdDetails.StdErr);
        }

        [TestMethod]
        [TestCategory("TargetSpace")]
        public void TargetSpace_ReturnsFalseResult_WhenCmdExitCodeIsNotZero()
        {
            var fakeSpaceName = "fake-space";
            string expectedCmdStr = $"\"{_fakePathToCfExe}\" {CfCliService.V6_TargetSpaceCmd} {fakeSpaceName}";
            CmdResult fakeFailureResult = new CmdResult(_fakeStdOut, _fakeStdErr, 1);

            mockCmdProcessService.Setup(mock => mock.
              ExecuteWindowlessCommand(expectedCmdStr, null))
                .Returns(fakeFailureResult);

            DetailedResult result = _sut.TargetSpace(fakeSpaceName);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.CmdDetails.ExitCode == 1);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdDetails.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdDetails.StdErr);
        }



        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppByNameAsync_ReturnsTrueResult_WhenCmdExitCodeIsZero()
        {
            var fakeAppName = "fake app name with spaces";
            string expectedArgs = $"{CfCliService.V6_StopAppCmd} \"{fakeAppName}\""; // expect app name to be surrounded by double quotes
            CmdResult fakeSuccessResult = new CmdResult(_fakeStdOut, _fakeStdErr, 0);

            mockCmdProcessService.Setup(mock => mock.
              RunCommand(_fakePathToCfExe, expectedArgs, null, null, null))
                .Returns(fakeSuccessResult);

            DetailedResult result = await _sut.StopAppByNameAsync(fakeAppName);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.CmdDetails.ExitCode == 0);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdDetails.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdDetails.StdErr);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppByNameAsync_ReturnsFalseResult_WhenCmdExitCodeIsNotZero()
        {
            var fakeAppName = "fake app name with spaces";
            string expectedArgs = $"{CfCliService.V6_StopAppCmd} \"{fakeAppName}\""; // expect app name to be surrounded by double quotes
            CmdResult fakeFailureResult = new CmdResult(_fakeStdOut, _fakeStdErr, 1);

            mockCmdProcessService.Setup(mock => mock.
              RunCommand(_fakePathToCfExe, expectedArgs, null, null, null))
                .Returns(fakeFailureResult);

            DetailedResult result = await _sut.StopAppByNameAsync(fakeAppName);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.CmdDetails.ExitCode == 1);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdDetails.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdDetails.StdErr);
        }



        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppByNameAsync_ReturnsTrueResult_WhenCmdExitCodeIsZero()
        {
            var fakeAppName = "fake app name with spaces";
            string expectedCmdStr = $"{CfCliService.V6_StartAppCmd} \"{fakeAppName}\""; // expect app name to be surrounded by double quotes
            CmdResult fakeSuccessResult = new CmdResult(_fakeStdOut, _fakeStdErr, 0);

            mockCmdProcessService.Setup(mock => mock.
              RunCommand(_fakePathToCfExe, expectedCmdStr, null, null, null))
                .Returns(fakeSuccessResult);

            DetailedResult result = await _sut.StartAppByNameAsync(fakeAppName);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.CmdDetails.ExitCode == 0);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdDetails.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdDetails.StdErr);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppByNameAsync_ReturnsFalseResult_WhenCmdExitCodeIsNotZero()
        {
            var fakeAppName = "fake app name with spaces";
            string expectedCmdStr = $"{CfCliService.V6_StartAppCmd} \"{fakeAppName}\""; // expect app name to be surrounded by double quotes
            CmdResult fakeFailureResult = new CmdResult(_fakeStdOut, _fakeStdErr, 1);

            mockCmdProcessService.Setup(mock => mock.
              RunCommand(_fakePathToCfExe, expectedCmdStr, null, null, null))
                .Returns(fakeFailureResult);

            DetailedResult result = await _sut.StartAppByNameAsync(fakeAppName);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.CmdDetails.ExitCode == 1);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdDetails.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdDetails.StdErr);
        }



        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppByNameAsync_ReturnsTrueResult_WhenCmdExitCodeIsZero()
        {
            var fakeAppName = "fake-app";
            string expectedCmdStr = $"{CfCliService.V6_DeleteAppCmd} \"{fakeAppName}\" -r"; // expect app name to be surrounded by double quotes
            CmdResult fakeSuccessResult = new CmdResult(_fakeStdOut, _fakeStdErr, 0);

            mockCmdProcessService.Setup(mock => mock.
              RunCommand(_fakePathToCfExe, expectedCmdStr, null, null, null))
                .Returns(fakeSuccessResult);

            DetailedResult result = await _sut.DeleteAppByNameAsync(fakeAppName);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.CmdDetails.ExitCode == 0);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdDetails.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdDetails.StdErr);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppByNameAsync_ReturnsFalseResult_WhenCmdExitCodeIsNotZero()
        {
            var fakeAppName = "fake-app";
            string expectedCmdStr = $"{CfCliService.V6_DeleteAppCmd} \"{fakeAppName}\" -r"; // expect app name to be surrounded by double quotes
            CmdResult fakeFailureResult = new CmdResult(_fakeStdOut, _fakeStdErr, 1);

            mockCmdProcessService.Setup(mock => mock.
              RunCommand(_fakePathToCfExe, expectedCmdStr, null, null, null))
                .Returns(fakeFailureResult);

            DetailedResult result = await _sut.DeleteAppByNameAsync(fakeAppName);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.CmdDetails.ExitCode == 1);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdDetails.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdDetails.StdErr);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppByNameAsync_DoesNotIncludeDashRFlag_WhenRemoveMappedRoutesIsFalse()
        {
            var fakeAppName = "fake-app";
            string expectedCmdStr = $"{CfCliService.V6_DeleteAppCmd} \"{fakeAppName}\""; // expect app name to be surrounded by double quotes
            CmdResult fakeSuccessResult = new CmdResult(_fakeStdOut, _fakeStdErr, 0);

            mockCmdProcessService.Setup(mock => mock.
              RunCommand(_fakePathToCfExe, expectedCmdStr, null, null, null))
                .Returns(fakeSuccessResult);

            DetailedResult result = await _sut.DeleteAppByNameAsync(fakeAppName, removeMappedRoutes: false);

            mockCmdProcessService.VerifyAll();
        }


        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ReturnsSuccessResult_WhenCommandSucceeds()
        {
            var fakeAppName = "my fake app";
            string expectedArgs = $"push \"{fakeAppName}\""; // ensure app name gets surrounded by quotes

            mockCmdProcessService.Setup(m => m.
                RunCommand(_fakePathToCfExe, expectedArgs, fakeProjectPath, null, null))
                    .Returns(fakeSuccessCmdResult);

            var result = await _sut.PushAppAsync(fakeAppName, null, null, fakeProjectPath);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(fakeSuccessCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ReturnsFailureResult_WhenCfExeCannotBeFound()
        {
            var fakeAppName = "my fake app";

            mockFileLocatorService.SetupGet(m => m.
                FullPathToCfExe)
                    .Returns((string)null);

            var result = await _sut.PushAppAsync(fakeAppName, null, null, fakeProjectPath);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.CmdDetails);
            Assert.AreEqual("Unable to locate cf.exe.", result.Explanation);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ReturnsFailureResult_WhenCfCmdFails()
        {
            var fakeAppName = "my fake app";
            string expectedArgs = $"push \"{fakeAppName}\""; // ensure app name gets surrounded by quotes

            mockCmdProcessService.Setup(m => m.
                RunCommand(_fakePathToCfExe, expectedArgs, fakeProjectPath, null, null))
                    .Returns(fakeFailureCmdResult);

            var result = await _sut.PushAppAsync(fakeAppName, null, null, fakeProjectPath);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(fakeFailureCmdResult.StdErr, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ReturnsGenericErrorMessage_WhenCfCmdFailsWithoutStdErr()
        {
            var fakeAppName = "my fake app";
            string expectedArgs = $"push \"{fakeAppName}\""; // ensure app name gets surrounded by quotes
            CmdResult mockFailedResult = new CmdResult("Something went wrong but there's no StdErr!", "", 1);

            mockCmdProcessService.Setup(m => m.
                RunCommand(_fakePathToCfExe, expectedArgs, fakeProjectPath, null, null))
                    .Returns(mockFailedResult);

            var result = await _sut.PushAppAsync(fakeAppName, null, null, fakeProjectPath);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual($"Unable to execute `cf {expectedArgs}`.", result.Explanation);
            Assert.AreEqual(mockFailedResult, result.CmdDetails);
        }


        [TestMethod]
        [TestCategory("GetApiVersion")]
        public async Task GetApiVersion_ReturnsVersion_WhenApiCmdSucceeds()
        {
            var expectedArgs = "api";

            var fakeMajorVersion = 1;
            var fakeMinorVersion = 2;
            var fakePatchVersion = 3;

            string fakeCmdOutput = "api endpoint:   https://my.cool.api.com";
            fakeCmdOutput += $"\napi version:    {fakeMajorVersion}.{fakeMinorVersion}.{fakePatchVersion}\n";

            CmdResult mockCmdResult = new CmdResult(fakeCmdOutput, string.Empty, 0);

            mockCmdProcessService.Setup(m => m.
                RunCommand(_fakePathToCfExe, expectedArgs, null, null, null))
                    .Returns(mockCmdResult);

            Version result = await _sut.GetApiVersion();

            Assert.AreEqual(result.Major, fakeMajorVersion);
            Assert.AreEqual(result.Minor, fakeMinorVersion);
            Assert.AreEqual(result.Build, fakePatchVersion);
        }

        [TestMethod]
        [TestCategory("GetApiVersion")]
        public async Task GetApiVersion_ReturnsNull_WhenApiCmdFails()
        {
            var expectedArgs = "api";

            mockCmdProcessService.Setup(m => m.
                RunCommand(_fakePathToCfExe, expectedArgs, null, null, null))
                    .Returns(fakeFailureCmdResult);

            Version result = await _sut.GetApiVersion();

            Assert.IsNull(result);
        }

        [TestMethod]
        [TestCategory("GetApiVersion")]
        public async Task GetApiVersion_ReturnsNull_WhenOutputParsingFails()
        {
            var expectedArgs = "api";

            string unparsableContent = "junk response";
            CmdResult mockCmdResult = new CmdResult(unparsableContent, string.Empty, 0);

            mockCmdProcessService.Setup(m => m.
                RunCommand(_fakePathToCfExe, expectedArgs, null, null, null))
                    .Returns(fakeFailureCmdResult);

            Version result = await _sut.GetApiVersion();

            Assert.IsNull(result);
        }
    }
}