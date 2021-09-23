using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.CfCli.Models.Apps;
using Tanzu.Toolkit.Services.CfCli.Models.Orgs;
using Tanzu.Toolkit.Services.CfCli.Models.Spaces;
using Tanzu.Toolkit.Services.CmdProcess;
using Tanzu.Toolkit.Services.FileLocator;
using Tanzu.Toolkit.Services.Logging;
using static Tanzu.Toolkit.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.Services.Tests.CfCli
{
    [TestClass]
    public class CfCliServiceTests : CfCliServiceTestSupport
    {
        private CfCliService _sut;
        private static readonly string _fakeArguments = "fake args";
        private static readonly string _fakePathToCfExe = "this\\is\\a\\fake\\path";
        private static readonly string _fakeStdOut = "some output content";
        private static readonly string _fakeStdErr = "some error content";
        private static readonly string _fakeRealisticTokenOutput = $"bearer {_fakeAccessToken}\n";
        private static readonly CmdResult _fakeSuccessResult = new CmdResult("junk output", "junk error", 0);
        private static readonly CmdResult _fakeFailureResult = new CmdResult("junk output", "junk error", 1);
        private static readonly StdOutDelegate _fakeOutCallback = content => { };
        private static readonly StdErrDelegate _fakeErrCallback = content => { };
        private static readonly CmdResult _fakeOrgsCmdResult = new CmdResult(_fakeMultiPageOrgsOutput, string.Empty, 0);
        private static readonly CmdResult _fakeNoOrgsCmdResult = new CmdResult(_fakeNoOrgsOutput, string.Empty, 0);
        private static readonly CmdResult _fakeSpacesCmdResult = new CmdResult(_fakeMultiPageSpacesOutput, string.Empty, 0);
        private static readonly CmdResult _fakeNoSpacesCmdResult = new CmdResult(_fakeNoSpacesOutput, string.Empty, 0);
        private static readonly CmdResult _fakeAppsCmdResult = new CmdResult(_fakeManyAppsOutput, string.Empty, 0);
        private static readonly CmdResult _fakeNoAppsCmdResult = new CmdResult(_fakeNoAppsOutput, string.Empty, 0);
        private static readonly string _fakeCfCliConfigFilePath = "this\\is\\a\\fake\\path";
        private static readonly Dictionary<string, string> _defaultEnvVars = new Dictionary<string, string> { { "CF_HOME", _fakeCfCliConfigFilePath } };

        private IServiceProvider _services;

        private Mock<ICfCliService> _mockCfCliService;
        private Mock<ICmdProcessService> _mockCmdProcessService;
        private Mock<IFileLocatorService> _mockFileLocatorService;
        private Mock<ILoggingService> _mockLoggingService;
        private Mock<ILogger> _mockLogger;

        [TestInitialize]
        public void TestInit()
        {
            var serviceCollection = new ServiceCollection();
            _mockCfCliService = new Mock<ICfCliService>();
            _mockCmdProcessService = new Mock<ICmdProcessService>();
            _mockFileLocatorService = new Mock<IFileLocatorService>();
            _mockLoggingService = new Mock<ILoggingService>();

            _mockLogger = new Mock<ILogger>();
            _mockLoggingService.SetupGet(m => m.Logger).Returns(_mockLogger.Object);

            serviceCollection.AddSingleton(_mockCfCliService.Object);
            serviceCollection.AddSingleton(_mockCmdProcessService.Object);
            serviceCollection.AddSingleton(_mockFileLocatorService.Object);
            serviceCollection.AddSingleton(_mockLoggingService.Object);

            _services = serviceCollection.BuildServiceProvider();

            _mockFileLocatorService.SetupGet(mock => mock.FullPathToCfExe).Returns(_fakePathToCfExe);
            _sut = new CfCliService(_fakeCfCliConfigFilePath, _services);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _mockFileLocatorService.VerifyAll();
            _mockCmdProcessService.VerifyAll();
            _mockLogger.VerifyAll();
        }

        [TestMethod]
        [TestCategory("RunCfCommandAsync")]
        public async Task RunCfCommandAsync_ReturnsSuccessfulResult_WhenCmdProcessExitsWithZeroCode()
        {
            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, _fakeArguments, null, _defaultEnvVars, _fakeOutCallback, _fakeErrCallback))
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
            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, _fakeArguments, null, _defaultEnvVars, _fakeOutCallback, _fakeErrCallback))
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

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, _fakeArguments, null, _defaultEnvVars, _fakeOutCallback, _fakeErrCallback))
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
            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, _fakeArguments, null, _defaultEnvVars, _fakeOutCallback, _fakeErrCallback))
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
            _mockFileLocatorService.SetupGet(mock => mock.FullPathToCfExe).Returns((string)null);

            DetailedResult result = await _sut.RunCfCommandAsync(_fakeArguments);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains("Unable to locate cf.exe"));
        }

        [TestMethod]
        [TestCategory("RunCfCommandAsync")]
        public async Task RunCfCommandAsync_UsesDefaultDir_WhenNotSpecified()
        {
            string expectedWorkingDir = null;

            _mockCmdProcessService.Setup(mock => mock.
                RunExecutable(_fakePathToCfExe, _fakeArguments, expectedWorkingDir, _defaultEnvVars, It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>()))
                    .Returns(_fakeSuccessResult);

            DetailedResult result = await _sut.RunCfCommandAsync(_fakeArguments);

            _mockCmdProcessService.Verify(mock => mock.RunExecutable(_fakePathToCfExe, _fakeArguments, expectedWorkingDir, _defaultEnvVars, null, null), Times.Once());
        }

        [TestMethod]
        [TestCategory("RunCfCommandAsync")]
        public async Task RunCfCommandAsync_SetsCFHOMEEnvironmentVariable_ToConfigFilePath()
        {
            string expectedWorkingDir = null;
            var fakeConfigFilePath = "fake\\path";
            var expectedEnvVars = new Dictionary<string, string> { { "CF_HOME", fakeConfigFilePath } };

            var sut = new CfCliService(fakeConfigFilePath, _services);

            _mockCmdProcessService.Setup(mock => mock.
                RunExecutable(_fakePathToCfExe, _fakeArguments, expectedWorkingDir, expectedEnvVars, It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>()))
                    .Returns(_fakeSuccessResult);

            DetailedResult result = await sut.RunCfCommandAsync(_fakeArguments);

            _mockCmdProcessService.Verify(mock => mock.RunExecutable(_fakePathToCfExe, _fakeArguments, expectedWorkingDir, expectedEnvVars, null, null), Times.Once());
        }

        [TestMethod]
        [TestCategory("ExecuteCfCliCommand")]
        public void ExecuteCfCliCommand_ReturnsTrueResult_WhenProcessExitCodeIsZero()
        {
            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, _fakeArguments, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessResult);

            DetailedResult result = _sut.ExecuteCfCliCommand(_fakeArguments);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
        }

        [TestMethod]
        [TestCategory("ExecuteCfCliCommand")]
        public void ExecuteCfCliCommand_ReturnsFalseResult_WhenProcessExitCodeIsNotZero()
        {
            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, _fakeArguments, null, _defaultEnvVars, null, null))
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
            var fakeFailedResult = new CmdResult("FAILED this is a mock response", string.Empty, 1);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, _fakeArguments, null, _defaultEnvVars, null, null))
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
            var fakeFailedResult = new CmdResult("junk output", string.Empty, 1);
            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, _fakeArguments, null, _defaultEnvVars, null, null))
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
            _mockFileLocatorService.SetupGet(mock => mock.
              FullPathToCfExe)
                .Returns((string)null);

            DetailedResult result = _sut.ExecuteCfCliCommand(_fakeArguments);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(CfCliService._cfExePathErrorMsg, result.Explanation);
        }

        [TestMethod]
        [TestCategory("GetOAuthToken")]
        public void GetOAuthToken_ReturnsNull_WhenProcessExitsWithNonZeroCode()
        {
            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, CfCliService._getOAuthTokenCmd, null, _defaultEnvVars, null, null))
                .Returns(new CmdResult(_fakeStdOut, _fakeStdErr, exitCode: 1));

            var token = _sut.GetOAuthToken();

            Assert.IsNull(token);
        }

        [TestMethod]
        [TestCategory("GetOAuthToken")]
        public void GetOAuthToken_TrimsPrefix_WhenResultStartsWithBearer()
        {
            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, CfCliService._getOAuthTokenCmd, null, _defaultEnvVars, null, null))
                .Returns(new CmdResult(_fakeRealisticTokenOutput, _fakeStdErr, exitCode: 0));

            var token = _sut.GetOAuthToken();
            Assert.IsFalse(token.Contains("bearer"));
        }

        [TestMethod]
        [TestCategory("GetOAuthToken")]
        public void GetOAuthToken_RemovesNewlinesFromTokenResult()
        {
            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, CfCliService._getOAuthTokenCmd, null, _defaultEnvVars, null, null))
                .Returns(new CmdResult(_fakeRealisticTokenOutput, _fakeStdErr, exitCode: 0));

            var token = _sut.GetOAuthToken();

            Assert.IsFalse(token.Contains("\n"));
        }

        [TestMethod]
        [TestCategory("GetOAuthToken")]
        public void GetOAuthToken_CachesFirstTokenResult()
        {
            var fakeTokenResult = new CmdResult(_fakeAccessToken, "", 0);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, It.Is<string>(s => s.Contains(CfCliService._getOAuthTokenCmd)), null, _defaultEnvVars, null, null))
                .Returns(fakeTokenResult);

            var firstResult = _sut.GetOAuthToken();
            Assert.AreEqual(_fakeAccessToken, firstResult);
            Assert.AreEqual(1, _mockCmdProcessService.Invocations.Count);
            _mockCmdProcessService.Verify(m => m.
              RunExecutable(_fakePathToCfExe, It.Is<string>(s => s.Contains(CfCliService._getOAuthTokenCmd)), null, _defaultEnvVars, null, null),
                Times.Once);

            _mockCmdProcessService.Invocations.Clear();
            _mockCmdProcessService.Reset();
            Assert.AreEqual(0, _mockCmdProcessService.Invocations.Count);

            var secondResult = _sut.GetOAuthToken();
            Assert.AreEqual(_fakeAccessToken, secondResult);
            Assert.AreEqual(0, _mockCmdProcessService.Invocations.Count);
        }

        [TestMethod]
        [TestCategory("GetOAuthToken")]
        public void GetOAuthToken_ReturnsNull_AndLogsError_WhenJwtCannotBeDecoded()
        {
            var fakeTokenResult = new CmdResult("my.fake.jwt", "", 0);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, It.Is<string>(s => s.Contains(CfCliService._getOAuthTokenCmd)), null, _defaultEnvVars, null, null))
                .Returns(fakeTokenResult);

            Exception thrownException = null;
            string result = "this should become null";
            try
            {
                result = _sut.GetOAuthToken();
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNull(thrownException);
            Assert.IsNull(result);
            _mockLogger.Verify(m => m.Error(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetOAuthToken")]
        public void GetOAuthToken_ThrowsInvalidRefreshTokenException_WhenStdErrReportsInvalidToken()
        {
            var fakeTokenResult = new CmdResult("", CfCliService._invalidRefreshTokenError, 1);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, It.Is<string>(s => s.Contains(CfCliService._getOAuthTokenCmd)), null, _defaultEnvVars, null, null))
                .Returns(fakeTokenResult);

            Exception thrownException = null;
            try
            {
                _sut.GetOAuthToken();
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);
            Assert.IsTrue(thrownException is InvalidRefreshTokenException);
        }

        [TestMethod]
        [TestCategory("ClearCachedAccessToken")]
        public void ClearCachedAccessToken_MandatesFullTokenLookup_ForNextCallToGetOAuthToken()
        {
            var fakeTokenResult = new CmdResult(_fakeAccessToken, "", 0);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, It.Is<string>(s => s.Contains(CfCliService._getOAuthTokenCmd)), null, _defaultEnvVars, null, null))
                .Returns(fakeTokenResult);

            var firstResult = _sut.GetOAuthToken();
            Assert.AreEqual(_fakeAccessToken, firstResult);
            Assert.AreEqual(1, _mockCmdProcessService.Invocations.Count);
            _mockCmdProcessService.Verify(m => m.
              RunExecutable(_fakePathToCfExe, It.Is<string>(s => s.Contains(CfCliService._getOAuthTokenCmd)), null, _defaultEnvVars, null, null),
                Times.Once);

            _mockCmdProcessService.Invocations.Clear();
            _mockCmdProcessService.Reset();
            Assert.AreEqual(0, _mockCmdProcessService.Invocations.Count);

            var secondResult = _sut.GetOAuthToken();
            Assert.AreEqual(_fakeAccessToken, secondResult);
            Assert.AreEqual(0, _mockCmdProcessService.Invocations.Count);

            // Now a token has been cached -- clear cache & expect CmdProcessService to be invoked again to get a fresh oauth-token.
            _mockCmdProcessService.Invocations.Clear();
            _mockCmdProcessService.Reset();

            _sut.ClearCachedAccessToken();

            var thirdResult = _sut.GetOAuthToken();
            Assert.AreEqual(_fakeAccessToken, secondResult);
            Assert.AreEqual(1, _mockCmdProcessService.Invocations.Count);
            _mockCmdProcessService.Verify(m => m.
              RunExecutable(_fakePathToCfExe, It.Is<string>(s => s.Contains(CfCliService._getOAuthTokenCmd)), null, _defaultEnvVars, null, null),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("TargetApi")]
        public void TargetApi_ReturnsTrue_WhenCmdExitCodeIsZero()
        {
            var fakeApiAddress = "my.api.addr";
            bool skipSsl = true;
            string expectedArgs = $"{CfCliService._targetApiCmd} {fakeApiAddress} --skip-ssl-validation";

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
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
            string expectedArgs = $"{CfCliService._targetApiCmd} {fakeApiAddress} --skip-ssl-validation";

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
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
            string expectedArgs = $"{CfCliService._authenticateCmd} {fakeUsername} {fakeDecodedPw}";

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(new CmdResult(_fakeStdOut, _fakeStdErr, 0));

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
            string expectedArgs = $"{CfCliService._authenticateCmd} {fakeUsername} {fakeDecodedPw}";

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(new CmdResult(_fakeStdOut, _fakeStdErr, 1));

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
            string expectedArgs = $"{CfCliService._getOrgsCmd} -v";

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeOrgsCmdResult);

            DetailedResult<List<Org>> result = await _sut.GetOrgsAsync();

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeOrgsCmdResult, result.CmdDetails);

            Assert.AreEqual(_numOrgsInFakeResponse, result.Content.Count);
            Assert.AreEqual(_fakeOrgName1, result.Content[0].Entity.Name);
            Assert.AreEqual(_fakeOrgGuid1, result.Content[0].Metadata.Guid);
        }

        [TestMethod]
        [TestCategory("GetOrgsAsync")]
        public async Task GetOrgsAsync_ReturnsFailedResult_WhenCmdResultReportsFailure()
        {
            string expectedArgs = $"{CfCliService._getOrgsCmd} -v";

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeFailureCmdResult);

            DetailedResult<List<Org>> result = await _sut.GetOrgsAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(CfCliService._requestErrorMsg, result.Explanation);
            Assert.AreEqual(_fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetOrgsAsync")]
        public async Task GetOrgsAsync_ReturnsFailedResult_WhenJsonParsingFails()
        {
            string expectedArgs = $"{CfCliService._getOrgsCmd} -v";
            var fakeInvalidJsonOutput = $"REQUEST {CfCliService._getOrgsRequestPath} asdf RESPONSE asdf";
            var fakeFailureCmdResult = new CmdResult(fakeInvalidJsonOutput, string.Empty, 0);

            _mockCmdProcessService.Setup(mock => mock
              .RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(fakeFailureCmdResult);

            DetailedResult<List<Org>> result = await _sut.GetOrgsAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(CfCliService._jsonParsingErrorMsg, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetOrgsAsync")]
        public async Task GetOrgsAsync_ReturnsFailedResult_For401UnauthorizedResponse()
        {
            string expectedArgs = $"{CfCliService._getOrgsCmd} -v";
            var fakeFailureCmdResult = new CmdResult(_fakeOrgs401Output, string.Empty, 0);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakeProjectPath, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(fakeFailureCmdResult);

            DetailedResult<List<Org>> result = await _sut.GetOrgsAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(CfCliService._jsonParsingErrorMsg, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetOrgsAsync")]
        public async Task GetOrgsAsync_ReturnsSuccessfulResult_WhenResponseContainsNoOrgsFound()
        {
            string expectedArgs = $"{CfCliService._getOrgsCmd} -v";

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakeProjectPath, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeNoOrgsCmdResult);

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
            string expectedArgs = $"{CfCliService._getSpacesCmd} -v";

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakeProjectPath, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSpacesCmdResult);

            DetailedResult<List<Space>> result = await _sut.GetSpacesAsync();

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeSpacesCmdResult, result.CmdDetails);

            Assert.AreEqual(_numOrgsInFakeResponse, result.Content.Count);
            Assert.AreEqual(_fakeSpaceName1, result.Content[0].Entity.Name);
            Assert.AreEqual(_fakeSpaceGuid1, result.Content[0].Metadata.Guid);
        }

        [TestMethod]
        [TestCategory("GetSpacesAsync")]
        public async Task GetSpacesAsync_ReturnsFailedResult_WhenCmdResultReportsFailure()
        {
            string expectedArgs = $"{CfCliService._getSpacesCmd} -v";

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakeProjectPath, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeFailureCmdResult);

            DetailedResult<List<Space>> result = await _sut.GetSpacesAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(CfCliService._requestErrorMsg, result.Explanation);
            Assert.AreEqual(_fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetSpacesAsync")]
        public async Task GetSpacesAsync_ReturnsFailedResult_WhenJsonParsingFails()
        {
            string expectedArgs = $"{CfCliService._getSpacesCmd} -v";
            var fakeInvalidJsonOutput = $"REQUEST {CfCliService._getSpacesRequestPath} asdf RESPONSE asdf";
            var fakeFailureCmdResult = new CmdResult(fakeInvalidJsonOutput, string.Empty, 0);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakeProjectPath, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(fakeFailureCmdResult);

            DetailedResult<List<Space>> result = await _sut.GetSpacesAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(CfCliService._jsonParsingErrorMsg, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetSpacesAsync")]
        public async Task GetSpacesAsync_ReturnsFailedResult_WhenResponseContainsNoSpacesFound()
        {
            string expectedArgs = $"{CfCliService._getSpacesCmd} -v";

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakeProjectPath, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeNoSpacesCmdResult);

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
            string expectedArgs = $"{CfCliService._getSpacesCmd} -v";
            var fakeFailureCmdResult = new CmdResult(_fakeSpaces401Output, string.Empty, 0);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakeProjectPath, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(fakeFailureCmdResult);

            DetailedResult<List<Space>> result = await _sut.GetSpacesAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(CfCliService._jsonParsingErrorMsg, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetAppsAsync")]
        public async Task GetAppsAsync_ReturnsSuccessfulResult_WhenCmdSucceeds()
        {
            string expectedArgs = $"{CfCliService._getAppsCmd} -v";
            int numAppsInFakeResponse = 53;

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeAppsCmdResult);

            var result = await _sut.GetAppsAsync();

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeAppsCmdResult, result.CmdDetails);

            Assert.AreEqual(typeof(List<App>), result.Content.GetType());
            Assert.AreEqual(numAppsInFakeResponse, result.Content.Count);
            Assert.AreEqual(_fakeAppName1, result.Content[0].Name);
            Assert.AreEqual(_fakeAppGuid1, result.Content[0].Guid);
        }

        [TestMethod]
        [TestCategory("GetAppsAsync")]
        public async Task GetAppsAsync_ReturnsFailedResult_WhenCmdResultReportsFailure()
        {
            string expectedArgs = $"{CfCliService._getAppsCmd} -v";
            var fakeFailureCmdResult = new CmdResult(string.Empty, string.Empty, 1);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(fakeFailureCmdResult);

            var result = await _sut.GetAppsAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(CfCliService._requestErrorMsg, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetAppsAsync")]
        public async Task GetAppsAsync_ReturnsFailedResult_WhenJsonParsingFails()
        {
            string expectedArgs = $"{CfCliService._getAppsCmd} -v";
            var fakeInvalidJsonOutput = $"REQUEST {CfCliService._getAppsRequestPath} asdf RESPONSE asdf";
            var fakeFailureCmdResult = new CmdResult(fakeInvalidJsonOutput, string.Empty, 0);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(fakeFailureCmdResult);

            var result = await _sut.GetAppsAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(CfCliService._jsonParsingErrorMsg, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetAppsAsync")]
        public async Task GetAppsAsync_ReturnsFailedResult_WhenResponseContainsNoAppsFound()
        {
            string expectedArgs = $"{CfCliService._getAppsCmd} -v";

            _mockCmdProcessService.Setup(mock => mock.
                RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                    .Returns(_fakeNoAppsCmdResult);

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
            string expectedArgs = $"{CfCliService._getAppsCmd} -v";
            var fakeFailureCmdResult = new CmdResult(_fakeApps401Output, string.Empty, 0);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(fakeFailureCmdResult);

            DetailedResult<List<App>> result = await _sut.GetAppsAsync();

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(CfCliService._jsonParsingErrorMsg, result.Explanation);
            Assert.AreEqual(fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("TargetOrg")]
        public void TargetOrg_ReturnsTrue_WhenCmdExitCodeIsZero()
        {
            var fakeOrgName = "fake org";
            string expectedArgs = $"{CfCliService._targetOrgCmd} \"{fakeOrgName}\""; // expect org name to be surrounded by double quotes
            CmdResult fakeSuccessResult = new CmdResult(_fakeStdOut, _fakeStdErr, 0);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(fakeSuccessResult);

            var result = _sut.TargetOrg(fakeOrgName);

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
            var fakeOrgName = "fake org";
            string expectedArgs = $"{CfCliService._targetOrgCmd} \"{fakeOrgName}\""; // expect org name to be surrounded by double quotes
            CmdResult fakeFailureResult = new CmdResult(_fakeStdOut, _fakeStdErr, 1);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(fakeFailureResult);

            var result = _sut.TargetOrg(fakeOrgName);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.CmdDetails.ExitCode == 1);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdDetails.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdDetails.StdErr);
        }

        [TestMethod]
        [TestCategory("TargetOrg")]
        public void TargetOrg_ThrowsInvalidRefreshTokenException_WhenStdErrReportsInvalidToken()
        {
            var fakeOrgName = "fake org";
            string expectedArgs = $"{CfCliService._targetOrgCmd} \"{fakeOrgName}\""; // expect org name to be surrounded by double quotes
            var tokenFailureResult = new CmdResult("junk", CfCliService._invalidRefreshTokenError, 1);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(tokenFailureResult);

            Exception thrownException = null;
            try
            {
                _sut.TargetOrg(fakeOrgName);
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);
            Assert.IsTrue(thrownException is InvalidRefreshTokenException);
        }

        [TestMethod]
        [TestCategory("TargetSpace")]
        public void TargetSpace_ReturnsTrueResult_WhenCmdExitCodeIsZero()
        {
            var fakeSpaceName = "fake-space";
            string expectedArgs = $"{CfCliService._targetSpaceCmd} \"{fakeSpaceName}\""; // ensure space name gets surrounded by quotes 
            CmdResult fakeSuccessResult = new CmdResult(_fakeStdOut, _fakeStdErr, 0);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(fakeSuccessResult);

            var result = _sut.TargetSpace(fakeSpaceName);

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
            string expectedArgs = $"{CfCliService._targetSpaceCmd} \"{fakeSpaceName}\""; // ensure space name gets surrounded by quotes
            CmdResult fakeFailureResult = new CmdResult(_fakeStdOut, _fakeStdErr, 1);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(fakeFailureResult);

            var result = _sut.TargetSpace(fakeSpaceName);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.CmdDetails.ExitCode == 1);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdDetails.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdDetails.StdErr);
        }

        [TestMethod]
        [TestCategory("TargetSpace")]
        public void TargetSpace_ThrowsInvalidRefreshTokenException_WhenStdErrReportsInvalidToken()
        {
            var fakeSpaceName = "fake-space";
            string expectedArgs = $"{CfCliService._targetSpaceCmd} \"{fakeSpaceName}\""; // ensure space name gets surrounded by quotes
            var tokenFailureResult = new CmdResult("junk", CfCliService._invalidRefreshTokenError, 1);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                .Returns(tokenFailureResult);

            Exception thrownException = null;
            try
            {
                _sut.TargetSpace(fakeSpaceName);
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);
            Assert.IsTrue(thrownException is InvalidRefreshTokenException);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppByNameAsync_ReturnsTrueResult_WhenCmdExitCodeIsZero()
        {
            var fakeAppName = "fake app name with spaces";
            string expectedArgs = $"{CfCliService._stopAppCmd} \"{fakeAppName}\""; // expect app name to be surrounded by double quotes
            CmdResult fakeSuccessResult = new CmdResult(_fakeStdOut, _fakeStdErr, 0);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
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
            string expectedArgs = $"{CfCliService._stopAppCmd} \"{fakeAppName}\""; // expect app name to be surrounded by double quotes
            CmdResult fakeFailureResult = new CmdResult(_fakeStdOut, _fakeStdErr, 1);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
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
            string expectedCmdStr = $"{CfCliService._startAppCmd} \"{fakeAppName}\""; // expect app name to be surrounded by double quotes
            CmdResult fakeSuccessResult = new CmdResult(_fakeStdOut, _fakeStdErr, 0);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedCmdStr, null, _defaultEnvVars, null, null))
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
            string expectedCmdStr = $"{CfCliService._startAppCmd} \"{fakeAppName}\""; // expect app name to be surrounded by double quotes
            CmdResult fakeFailureResult = new CmdResult(_fakeStdOut, _fakeStdErr, 1);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedCmdStr, null, _defaultEnvVars, null, null))
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
            string expectedCmdStr = $"{CfCliService._deleteAppCmd} \"{fakeAppName}\" -r"; // expect app name to be surrounded by double quotes
            CmdResult fakeSuccessResult = new CmdResult(_fakeStdOut, _fakeStdErr, 0);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedCmdStr, null, _defaultEnvVars, null, null))
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
            string expectedCmdStr = $"{CfCliService._deleteAppCmd} \"{fakeAppName}\" -r"; // expect app name to be surrounded by double quotes
            CmdResult fakeFailureResult = new CmdResult(_fakeStdOut, _fakeStdErr, 1);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedCmdStr, null, _defaultEnvVars, null, null))
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
            string expectedCmdStr = $"{CfCliService._deleteAppCmd} \"{fakeAppName}\""; // expect app name to be surrounded by double quotes
            CmdResult fakeSuccessResult = new CmdResult(_fakeStdOut, _fakeStdErr, 0);

            _mockCmdProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedCmdStr, null, _defaultEnvVars, null, null))
                .Returns(fakeSuccessResult);

            DetailedResult result = await _sut.DeleteAppByNameAsync(fakeAppName, removeMappedRoutes: false);

            _mockCmdProcessService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ReturnsSuccessResult_WhenCommandSucceeds()
        {
            var fakeAppName = "my fake app";
            string expectedArgs = $"push \"{fakeAppName}\""; // ensure app name gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, _fakeProjectPath, _defaultEnvVars, null, null))
                    .Returns(_fakeSuccessCmdResult);

            var result = await _sut.PushAppAsync(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName, null, null, _fakeProjectPath, null, null, null);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeSuccessCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_AddsSFlag_WhenGivenAStackParam()
        {
            var fakeAppName = "my fake app";
            var fakeStackValue = "my-cool-stack-name";
            string expectedArgs = $"push \"{fakeAppName}\" -s {fakeStackValue}"; // ensure app name gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes

            _mockCmdProcessService.Setup(m => m.
               RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, _fakeProjectPath, _defaultEnvVars, null, null))
                    .Returns(_fakeSuccessCmdResult);

            var result = await _sut.PushAppAsync(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName, null, null, _fakeProjectPath, stack: fakeStackValue, manifestPath: null, startCommand: null);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeSuccessCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_AddsBFlag_WhenGivenABuildpackParam()
        {
            var fakeAppName = "my fake app";
            var fakeBuildpackValue = "my-cool-buildpack";
            string expectedArgs = $"push \"{fakeAppName}\" -b {fakeBuildpackValue}"; // ensure app name gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, _fakeProjectPath, _defaultEnvVars, null, null))
                    .Returns(_fakeSuccessCmdResult);

            var result = await _sut.PushAppAsync(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName, null, null, _fakeProjectPath, buildpack: fakeBuildpackValue, manifestPath: null, startCommand: null);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeSuccessCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_AddsDashFArgument_WhenGivenAManifestPathParam()
        {
            var fakeAppName = "my fake app";
            var fakeManifestPath = "this\\is\\a\\fake\\path\\to\\manifest.yml";
            string expectedArgs = $"push \"{fakeAppName}\" -f \"{fakeManifestPath}\""; // ensure manifest path gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, _fakeProjectPath, _defaultEnvVars, null, null))
                    .Returns(_fakeSuccessCmdResult);

            var result = await _sut.PushAppAsync(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName, null, null, _fakeProjectPath, manifestPath: fakeManifestPath, startCommand: null);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeSuccessCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_AddsCFlag_WhenGivenAStartCommandParam()
        {
            var fakeAppName = "my fake app";
            var fakeStartCommand = "just DO it!";
            string expectedArgs = $"push \"{fakeAppName}\" -c \"{fakeStartCommand}\""; // ensure app name gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, _fakeProjectPath, _defaultEnvVars, null, null))
                    .Returns(_fakeSuccessCmdResult);

            var result = await _sut.PushAppAsync(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName, null, null, _fakeProjectPath, buildpack: null, manifestPath: null, startCommand: fakeStartCommand);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeSuccessCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_AddsMultipleFlags_WhenGivenMultipleOptionalParams()
        {
            var fakeAppName = "my fake app";
            var fakeStackValue = "my-cool-stack";
            var fakeStartCommand = "run the thing!";
            var fakeManifestPath = "this\\is\\a\\fake\\path\\to\\manifest.yml";
            var fakeBuildpackValue = "my-cool-buildpack";

            // ensure app name, start command & manifest path get surrounded by quotes
            string expectedArgs = $"push \"{fakeAppName}\" -b {fakeBuildpackValue} -s {fakeStackValue} -c \"{fakeStartCommand}\" -f \"{fakeManifestPath}\"";
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, _fakeProjectPath, _defaultEnvVars, null, null))
                    .Returns(_fakeSuccessCmdResult);

            var result = await _sut.PushAppAsync(fakeAppName,
                                                 FakeOrg.OrgName,
                                                 FakeSpace.SpaceName,
                                                 null,
                                                 null,
                                                 _fakeProjectPath,
                                                 buildpack: fakeBuildpackValue,
                                                 stack: fakeStackValue,
                                                 startCommand: fakeStartCommand,
                                                 manifestPath: fakeManifestPath);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeSuccessCmdResult, result.CmdDetails);
        }

        [TestCategory("PushApp")]
        public async Task PushAppAsync_ReturnsFailureResult_WhenCfExeCannotBeFound()
        {
            var fakeAppName = "my fake app";

            _mockFileLocatorService.SetupGet(m => m.
                FullPathToCfExe)
                    .Returns((string)null);

            var result = await _sut.PushAppAsync(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName, null, null, _fakeProjectPath, null, null, null);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.CmdDetails);
            Assert.AreEqual("Unable to locate cf.exe.", result.Explanation);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ReturnsFailureResult_WhenTargetOrgFails()
        {
            var fakeAppName = "my fake app";
            string expectedArgs = $"push \"{fakeAppName}\""; // ensure app name gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeFailureCmdResult);

            var result = await _sut.PushAppAsync(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName, null, null, _fakeProjectPath, null, null, null);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains("Unable to deploy"));
            Assert.IsTrue(result.Explanation.Contains(fakeAppName));
            Assert.IsTrue(result.Explanation.Contains("failed to target org"));
            Assert.IsTrue(result.Explanation.Contains(FakeOrg.OrgName));
            Assert.AreEqual(_fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ReturnsFailureResult_WhenTargetSpaceFails()
        {
            var fakeAppName = "my fake app";
            string expectedArgs = $"push \"{fakeAppName}\""; // ensure app name gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeFailureCmdResult);

            var result = await _sut.PushAppAsync(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName, null, null, _fakeProjectPath, null, null, null);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains("Unable to deploy"));
            Assert.IsTrue(result.Explanation.Contains(fakeAppName));
            Assert.IsTrue(result.Explanation.Contains("failed to target space"));
            Assert.IsTrue(result.Explanation.Contains(FakeSpace.SpaceName));
            Assert.AreEqual(_fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ReturnsFailureResult_WhenCfCmdFails()
        {
            var fakeAppName = "my fake app";
            string expectedArgs = $"push \"{fakeAppName}\""; // ensure app name gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, _fakeProjectPath, _defaultEnvVars, null, null))
                    .Returns(_fakeFailureCmdResult);

            var result = await _sut.PushAppAsync(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName, null, null, _fakeProjectPath, null, null, null);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(_fakeFailureCmdResult.StdErr, result.Explanation);
            Assert.AreEqual(_fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ReturnsGenericErrorMessage_WhenCfCmdFailsWithoutStdErr()
        {
            var fakeAppName = "my fake app";
            string expectedArgs = $"push \"{fakeAppName}\""; // ensure app name gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes
            CmdResult mockFailedResult = new CmdResult("Something went wrong but there's no StdErr!", "", 1);

            _mockCmdProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, _fakeProjectPath, _defaultEnvVars, null, null))
                    .Returns(mockFailedResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            var result = await _sut.PushAppAsync(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName, null, null, _fakeProjectPath, null, null, null);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual($"Unable to execute `cf {expectedArgs}`.", result.Explanation);
            Assert.AreEqual(mockFailedResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ThrowsInvalidRefreshTokenException_WhenTargetOrgReportsInvalidRefreshToken()
        {
            var fakeAppName = "my fake app";
            string expectedArgs = $"push \"{fakeAppName}\""; // ensure app name gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var invalidRefreshTokenCmdResult = new CmdResult("junk output", CfCliService._invalidRefreshTokenError, 1);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(invalidRefreshTokenCmdResult);

            Exception thrownException = null;
            try
            {
                await _sut.PushAppAsync(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName, null, null, _fakeProjectPath, null, null, null);
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);
            Assert.IsTrue(thrownException is InvalidRefreshTokenException);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ThrowsInvalidRefreshTokenException_WhenTargetSpaceReportsInvalidRefreshToken()
        {
            var fakeAppName = "my fake app";
            string expectedArgs = $"push \"{fakeAppName}\""; // ensure app name gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes
            var invalidRefreshTokenCmdResult = new CmdResult("junk output", CfCliService._invalidRefreshTokenError, 1);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(invalidRefreshTokenCmdResult);

            Exception thrownException = null;
            try
            {
                await _sut.PushAppAsync(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName, null, null, _fakeProjectPath, null, null, null);
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);
            Assert.IsTrue(thrownException is InvalidRefreshTokenException);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ThrowsInvalidRefreshTokenException_WhenCfCmdFailsDueToInvalidRefreshToken()
        {
            var fakeAppName = "my fake app";
            string expectedArgs = $"push \"{fakeAppName}\""; // ensure app name gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes
            var invalidRefreshTokenCmdResult = new CmdResult("junk output", CfCliService._invalidRefreshTokenError, 1);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, _fakeProjectPath, _defaultEnvVars, null, null))
                    .Returns(invalidRefreshTokenCmdResult);

            Exception thrownException = null;
            try
            {
                await _sut.PushAppAsync(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName, null, null, _fakeProjectPath, null, null, null);
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);
            Assert.IsTrue(thrownException is InvalidRefreshTokenException);
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

            _mockCmdProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
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

            _mockCmdProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                    .Returns(_fakeFailureCmdResult);

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

            _mockCmdProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                    .Returns(_fakeFailureCmdResult);

            Version result = await _sut.GetApiVersion();

            Assert.IsNull(result);
        }

        [TestMethod]
        [TestCategory("GetRecentAppLogs")]
        public async Task GetRecentAppLogs_ReturnsSuccessResult_WhenLogsCmdSucceeds()
        {
            var fakeAppName = "fake app name";
            var expectedLogsCmdArgs = $"logs \"{fakeAppName}\" --recent"; // expect app name to be surrounded by double quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes

            string fakeCmdOutput = "These are fake app logs\nYabadabbadoo";

            CmdResult mockCmdResult = new CmdResult(fakeCmdOutput, string.Empty, 0);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedLogsCmdArgs, null, _defaultEnvVars, null, null))
                    .Returns(mockCmdResult);

            var result = await _sut.GetRecentAppLogs(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName);

            Assert.AreEqual(result.Content, fakeCmdOutput);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(mockCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetRecentAppLogs")]
        public async Task GetRecentAppLogs_ReturnsFailureResult_WhenLogsCmdExitCodeIsNotZero()
        {
            var fakeAppName = "fake app name";
            var expectedArgs = $"logs \"{fakeAppName}\" --recent"; // expect app name to be surrounded by double quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes

            string fakeCmdOutput = "These are fake app logs\nYabadabbadoo";

            var errorMsg = "junk err";
            CmdResult mockCmdResult = new CmdResult(fakeCmdOutput, errorMsg, 1);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null))
                    .Returns(mockCmdResult);

            var result = await _sut.GetRecentAppLogs(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(result.Content, fakeCmdOutput);
            Assert.AreEqual(mockCmdResult.StdErr, result.Explanation);
            Assert.AreEqual(mockCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetRecentAppLogs")]
        public async Task GetRecentAppLogs_ReturnsFailureResult_WhenTargetOrgFails()
        {
            var fakeAppName = "junk";
            var expectedArgs = $"logs {fakeAppName} --recent";
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeFailureCmdResult);

            var result = await _sut.GetRecentAppLogs(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(_fakeFailureCmdResult.StdErr, result.Explanation);
            Assert.AreEqual(_fakeFailureCmdResult, result.CmdDetails);
        }

        [TestMethod]
        [TestCategory("GetRecentAppLogs")]
        public async Task GetRecentAppLogs_ReturnsFailureResult_WhenTargetSpaceFails()
        {
            var fakeAppName = "junk";
            var expectedArgs = $"logs {fakeAppName} --recent";
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeFailureCmdResult);

            var result = await _sut.GetRecentAppLogs(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(_fakeFailureCmdResult.StdErr, result.Explanation);
            Assert.AreEqual(_fakeFailureCmdResult, result.CmdDetails);
        }


        [TestMethod]
        [TestCategory("GetRecentAppLogs")]
        public async Task GetRecentAppLogs_ThrowsInvalidRefreshTokenException_WhenTargetOrgReportsInvalidRefreshToken()
        {
            var fakeAppName = "my fake app";
            var expectedArgs = $"logs {fakeAppName} --recent";
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var invalidRefreshTokenCmdResult = new CmdResult("junk output", CfCliService._invalidRefreshTokenError, 1);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(invalidRefreshTokenCmdResult);

            Exception thrownException = null;
            try
            {
                await _sut.GetRecentAppLogs(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName);
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);
            Assert.IsTrue(thrownException is InvalidRefreshTokenException);
        }

        [TestMethod]
        [TestCategory("GetRecentAppLogs")]
        public async Task GetRecentAppLogs_ThrowsInvalidRefreshTokenException_WhenTargetSpaceReportsInvalidRefreshToken()
        {
            var fakeAppName = "my fake app";
            var expectedArgs = $"logs {fakeAppName} --recent";
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes
            var invalidRefreshTokenCmdResult = new CmdResult("junk output", CfCliService._invalidRefreshTokenError, 1);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(invalidRefreshTokenCmdResult);

            Exception thrownException = null;
            try
            {
                await _sut.GetRecentAppLogs(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName);
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);
            Assert.IsTrue(thrownException is InvalidRefreshTokenException);
        }

        [TestMethod]
        [TestCategory("GetRecentAppLogs")]
        public async Task GetRecentAppLogs_ThrowsInvalidRefreshTokenException_WhenCfCmdFailsDueToInvalidRefreshToken()
        {
            var fakeAppName = "my fake app";
            var expectedArgs = $"logs {fakeAppName} --recent";
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes
            var invalidRefreshTokenCmdResult = new CmdResult("junk output", CfCliService._invalidRefreshTokenError, 1);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCmdProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null))
                    .Returns(invalidRefreshTokenCmdResult);

            Exception thrownException = null;
            try
            {
                await _sut.GetRecentAppLogs(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName);
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);
            Assert.IsTrue(thrownException is InvalidRefreshTokenException);
        }

    }
}