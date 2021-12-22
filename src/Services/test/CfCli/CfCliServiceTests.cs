using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.CommandProcess;
using Tanzu.Toolkit.Services.File;
using Tanzu.Toolkit.Services.Logging;
using static Tanzu.Toolkit.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.Services.Tests.CfCli
{
    [TestClass]
    public class CfCliServiceTests : ServicesTestSupport
    {
        private CfCliService _sut;
        private static readonly string _fakeArguments = "fake args";
        private static readonly string _fakePathToCfExe = "this\\is\\a\\fake\\path";
        private static readonly string _fakeStdOut = "some output content";
        private static readonly string _fakeStdErr = "some error content";
        private static readonly string _fakeRealisticTokenOutput = $"bearer {_fakeAccessToken}\n";
        private static readonly CommandResult _fakeSuccessResult = new CommandResult("junk output", "junk error", 0);
        private static readonly CommandResult _fakeFailureResult = new CommandResult("junk output", "junk error", 1);
        private static readonly string _fakeCfCliConfigFilePath = "this\\is\\a\\fake\\path";
        private static readonly Dictionary<string, string> _defaultEnvVars = new Dictionary<string, string> { { "CF_HOME", _fakeCfCliConfigFilePath } };

        private IServiceProvider _services;

        private Mock<ICfCliService> _mockCfCliService;
        private Mock<ICommandProcessService> _mockCommandProcessService;
        private Mock<IFileService> _mockFileService;
        private Mock<ILoggingService> _mockLoggingService;
        private Mock<ILogger> _mockLogger;

        [TestInitialize]
        public void TestInit()
        {
            var serviceCollection = new ServiceCollection();
            _mockCfCliService = new Mock<ICfCliService>();
            _mockCommandProcessService = new Mock<ICommandProcessService>();
            _mockFileService = new Mock<IFileService>();
            _mockLoggingService = new Mock<ILoggingService>();

            _mockLogger = new Mock<ILogger>();
            _mockLoggingService.SetupGet(m => m.Logger).Returns(_mockLogger.Object);

            serviceCollection.AddSingleton(_mockCfCliService.Object);
            serviceCollection.AddSingleton(_mockCommandProcessService.Object);
            serviceCollection.AddSingleton(_mockFileService.Object);
            serviceCollection.AddSingleton(_mockLoggingService.Object);

            _services = serviceCollection.BuildServiceProvider();

            _mockFileService.SetupGet(mock => mock.FullPathToCfExe).Returns(_fakePathToCfExe);
            _sut = new CfCliService(_fakeCfCliConfigFilePath, _services);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _mockFileService.VerifyAll();
            _mockCommandProcessService.VerifyAll();
            _mockLogger.VerifyAll();
        }

        [TestMethod]
        [TestCategory("RunCfCommandAsync")]
        public async Task RunCfCommandAsync_ReturnsSuccessfulResult_WhenCommandProcessExitsWithZeroCode()
        {
            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, _fakeArguments, null, _defaultEnvVars, _fakeOutCallback, _fakeErrCallback, null))
                .Returns(new CommandResult(_fakeStdOut, _fakeStdErr, 0));

            var result = await _sut.RunCfCommandAsync(_fakeArguments, _fakeOutCallback, _fakeErrCallback, null);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.IsTrue(result.CmdResult.ExitCode == 0);
            Assert.IsTrue(result.CmdResult.StdOut == _fakeStdOut);
            Assert.IsTrue(result.CmdResult.StdErr == _fakeStdErr);
        }

        [TestMethod]
        [TestCategory("RunCfCommandAsync")]
        public async Task RunCfCommandAsync_ReturnsFailedResult_WhenCommandProcessExitsWithNonZeroCode()
        {
            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, _fakeArguments, null, _defaultEnvVars, _fakeOutCallback, _fakeErrCallback, null))
                .Returns(new CommandResult(_fakeStdOut, _fakeStdErr, 1));

            var result = await _sut.RunCfCommandAsync(_fakeArguments, _fakeOutCallback, _fakeErrCallback, null);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(_fakeStdErr));
            Assert.IsTrue(result.CmdResult.ExitCode == 1);
            Assert.IsTrue(result.CmdResult.StdOut == _fakeStdOut);
            Assert.IsTrue(result.CmdResult.StdErr == _fakeStdErr);
        }

        [TestMethod]
        [TestCategory("RunCfCommandAsync")]
        public async Task RunCfCommandAsync_ReturnsStdOut_WhenProcessFailsWithoutStdErr_AndStdOutContainsFAILEDSubstring()
        {
            const string mockStdOutContainingFailedSubstring = "FAILED this is a mock response";

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, _fakeArguments, null, _defaultEnvVars, _fakeOutCallback, _fakeErrCallback, null))
                .Returns(new CommandResult(mockStdOutContainingFailedSubstring, string.Empty, 1));

            var result = await _sut.RunCfCommandAsync(_fakeArguments, _fakeOutCallback, _fakeErrCallback, null);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(mockStdOutContainingFailedSubstring));
            Assert.IsTrue(result.CmdResult.ExitCode == 1);
            Assert.IsTrue(result.CmdResult.StdOut == mockStdOutContainingFailedSubstring);
            Assert.IsTrue(result.CmdResult.StdErr == string.Empty);
        }

        [TestMethod]
        [TestCategory("RunCfCommandAsync")]
        public async Task RunCfCommandAsync_ReturnsGenericExplanation_WhenProcessFailsWithoutStdErr()
        {
            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, _fakeArguments, null, _defaultEnvVars, _fakeOutCallback, _fakeErrCallback, null))
                .Returns(new CommandResult(_fakeStdOut, string.Empty, 1));

            var result = await _sut.RunCfCommandAsync(_fakeArguments, _fakeOutCallback, _fakeErrCallback, null);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains($"Unable to execute `cf {_fakeArguments}`."));
            Assert.IsTrue(result.CmdResult.ExitCode == 1);
            Assert.IsTrue(result.CmdResult.StdOut == _fakeStdOut);
            Assert.IsTrue(result.CmdResult.StdErr == string.Empty);
        }

        [TestMethod]
        [TestCategory("RunCfCommandAsync")]
        public async Task RunCfCommandAsync_ReturnsFalseResult_WhenCfExeCouldNotBeFound()
        {
            _mockFileService.SetupGet(mock => mock.FullPathToCfExe).Returns((string)null);

            DetailedResult result = await _sut.RunCfCommandAsync(_fakeArguments, null);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains("Unable to locate cf.exe"));
        }

        [TestMethod]
        [TestCategory("RunCfCommandAsync")]
        public async Task RunCfCommandAsync_UsesDefaultDir_WhenNotSpecified()
        {
            string expectedWorkingDir = null;

            _mockCommandProcessService.Setup(mock => mock.
                RunExecutable(_fakePathToCfExe, _fakeArguments, expectedWorkingDir, _defaultEnvVars, It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>(), null))
                    .Returns(_fakeSuccessResult);

            DetailedResult result = await _sut.RunCfCommandAsync(_fakeArguments, null);

            _mockCommandProcessService.Verify(mock => mock.RunExecutable(_fakePathToCfExe, _fakeArguments, expectedWorkingDir, _defaultEnvVars, null, null, null), Times.Once());
        }

        [TestMethod]
        [TestCategory("RunCfCommandAsync")]
        public async Task RunCfCommandAsync_SetsCFHOMEEnvironmentVariable_ToConfigFilePath()
        {
            string expectedWorkingDir = null;
            var fakeConfigFilePath = "fake\\path";
            var expectedEnvVars = new Dictionary<string, string> { { "CF_HOME", fakeConfigFilePath } };

            var sut = new CfCliService(fakeConfigFilePath, _services);

            _mockCommandProcessService.Setup(mock => mock.
                RunExecutable(_fakePathToCfExe, _fakeArguments, expectedWorkingDir, expectedEnvVars, It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>(), null))
                    .Returns(_fakeSuccessResult);

            DetailedResult result = await sut.RunCfCommandAsync(_fakeArguments, null);

            _mockCommandProcessService.Verify(mock => mock.RunExecutable(_fakePathToCfExe, _fakeArguments, expectedWorkingDir, expectedEnvVars, null, null, null), Times.Once());
        }

        [TestMethod]
        [TestCategory("ExecuteCfCliCommand")]
        public void ExecuteCfCliCommand_ReturnsTrueResult_WhenProcessExitCodeIsZero()
        {
            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, _fakeArguments, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessResult);

            DetailedResult result = _sut.ExecuteCfCliCommand(_fakeArguments);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
        }

        [TestMethod]
        [TestCategory("ExecuteCfCliCommand")]
        public void ExecuteCfCliCommand_ReturnsFalseResult_WhenProcessExitCodeIsNotZero()
        {
            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, _fakeArguments, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeFailureResult);

            DetailedResult result = _sut.ExecuteCfCliCommand(_fakeArguments);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(_fakeFailureResult.StdErr));
            Assert.AreEqual(_fakeFailureResult, result.CmdResult);
            Assert.IsTrue(result.CmdResult.StdErr == _fakeFailureResult.StdErr);
        }

        [TestMethod]
        [TestCategory("ExecuteCfCliCommand")]
        public void ExecuteCfCliCommand_ReturnsStdOut_WhenProcessFailsWithoutStdErr_AndStdOutContainsFAILEDSubstring()
        {
            var fakeFailedResult = new CommandResult("FAILED this is a mock response", string.Empty, 1);

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, _fakeArguments, null, _defaultEnvVars, null, null, null))
                .Returns(fakeFailedResult);

            DetailedResult result = _sut.ExecuteCfCliCommand(_fakeArguments);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains(fakeFailedResult.StdOut));
            Assert.AreEqual(fakeFailedResult, result.CmdResult);
            Assert.IsTrue(result.CmdResult.StdErr == string.Empty);
        }

        [TestMethod]
        [TestCategory("ExecuteCfCliCommand")]
        public void ExecuteCfCliCommand_ReturnsGenericExplanation_WhenProcessFailsWithoutStdErr()
        {
            var fakeFailedResult = new CommandResult("junk output", string.Empty, 1);
            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, _fakeArguments, null, _defaultEnvVars, null, null, null))
                .Returns(fakeFailedResult);

            DetailedResult result = _sut.ExecuteCfCliCommand(_fakeArguments);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains($"Unable to execute `cf {_fakeArguments}`."));
            Assert.AreEqual(fakeFailedResult, result.CmdResult);
            Assert.IsTrue(result.CmdResult.StdErr == string.Empty);
        }

        [TestMethod]
        [TestCategory("ExecuteCfCliCommand")]
        public void ExecuteCfCliCommand_ReturnsFalseResult_WhenCfExeCouldNotBeFound()
        {
            _mockFileService.SetupGet(mock => mock.
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
            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, CfCliService._getOAuthTokenCmd, null, _defaultEnvVars, null, null, null))
                .Returns(new CommandResult(_fakeStdOut, _fakeStdErr, exitCode: 1));

            var token = _sut.GetOAuthToken();

            Assert.IsNull(token);
        }

        [TestMethod]
        [TestCategory("GetOAuthToken")]
        public void GetOAuthToken_TrimsPrefix_WhenResultStartsWithBearer()
        {
            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, CfCliService._getOAuthTokenCmd, null, _defaultEnvVars, null, null, null))
                .Returns(new CommandResult(_fakeRealisticTokenOutput, _fakeStdErr, exitCode: 0));

            var token = _sut.GetOAuthToken();
            Assert.IsFalse(token.Contains("bearer"));
        }

        [TestMethod]
        [TestCategory("GetOAuthToken")]
        public void GetOAuthToken_RemovesNewlinesFromTokenResult()
        {
            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, CfCliService._getOAuthTokenCmd, null, _defaultEnvVars, null, null, null))
                .Returns(new CommandResult(_fakeRealisticTokenOutput, _fakeStdErr, exitCode: 0));

            var token = _sut.GetOAuthToken();

            Assert.IsFalse(token.Contains("\n"));
        }

        [TestMethod]
        [TestCategory("GetOAuthToken")]
        public void GetOAuthToken_CachesFirstTokenResult()
        {
            var fakeTokenResult = new CommandResult(_fakeAccessToken, "", 0);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, It.Is<string>(s => s.Contains(CfCliService._getOAuthTokenCmd)), null, _defaultEnvVars, null, null, null))
                .Returns(fakeTokenResult);

            var firstResult = _sut.GetOAuthToken();
            Assert.AreEqual(_fakeAccessToken, firstResult);
            Assert.AreEqual(1, _mockCommandProcessService.Invocations.Count);
            _mockCommandProcessService.Verify(m => m.
              RunExecutable(_fakePathToCfExe, It.Is<string>(s => s.Contains(CfCliService._getOAuthTokenCmd)), null, _defaultEnvVars, null, null, null),
                Times.Once);

            _mockCommandProcessService.Invocations.Clear();
            _mockCommandProcessService.Reset();
            Assert.AreEqual(0, _mockCommandProcessService.Invocations.Count);

            var secondResult = _sut.GetOAuthToken();
            Assert.AreEqual(_fakeAccessToken, secondResult);
            Assert.AreEqual(0, _mockCommandProcessService.Invocations.Count);
        }

        [TestMethod]
        [TestCategory("GetOAuthToken")]
        public void GetOAuthToken_ReturnsNull_AndLogsError_WhenJwtCannotBeDecoded()
        {
            var fakeTokenResult = new CommandResult("my.fake.jwt", "", 0);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, It.Is<string>(s => s.Contains(CfCliService._getOAuthTokenCmd)), null, _defaultEnvVars, null, null, null))
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
            var fakeTokenResult = new CommandResult("", CfCliService._invalidRefreshTokenError, 1);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, It.Is<string>(s => s.Contains(CfCliService._getOAuthTokenCmd)), null, _defaultEnvVars, null, null, null))
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
            var fakeTokenResult = new CommandResult(_fakeAccessToken, "", 0);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, It.Is<string>(s => s.Contains(CfCliService._getOAuthTokenCmd)), null, _defaultEnvVars, null, null, null))
                .Returns(fakeTokenResult);

            var firstResult = _sut.GetOAuthToken();
            Assert.AreEqual(_fakeAccessToken, firstResult);
            Assert.AreEqual(1, _mockCommandProcessService.Invocations.Count);
            _mockCommandProcessService.Verify(m => m.
              RunExecutable(_fakePathToCfExe, It.Is<string>(s => s.Contains(CfCliService._getOAuthTokenCmd)), null, _defaultEnvVars, null, null, null),
                Times.Once);

            _mockCommandProcessService.Invocations.Clear();
            _mockCommandProcessService.Reset();
            Assert.AreEqual(0, _mockCommandProcessService.Invocations.Count);

            var secondResult = _sut.GetOAuthToken();
            Assert.AreEqual(_fakeAccessToken, secondResult);
            Assert.AreEqual(0, _mockCommandProcessService.Invocations.Count);

            // Now a token has been cached -- clear cache & expect CommandProcessService to be invoked again to get a fresh oauth-token.
            _mockCommandProcessService.Invocations.Clear();
            _mockCommandProcessService.Reset();

            _sut.ClearCachedAccessToken();

            var thirdResult = _sut.GetOAuthToken();
            Assert.AreEqual(_fakeAccessToken, secondResult);
            Assert.AreEqual(1, _mockCommandProcessService.Invocations.Count);
            _mockCommandProcessService.Verify(m => m.
              RunExecutable(_fakePathToCfExe, It.Is<string>(s => s.Contains(CfCliService._getOAuthTokenCmd)), null, _defaultEnvVars, null, null, null),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("TargetApi")]
        public void TargetApi_ReturnsTrue_WhenCmdExitCodeIsZero()
        {
            var fakeApiAddress = "my.api.addr";
            bool skipSsl = true;
            string expectedArgs = $"{CfCliService._targetApiCmd} {fakeApiAddress} --skip-ssl-validation";

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null, null))
                .Returns(new CommandResult(_fakeStdOut, _fakeStdErr, 0));

            DetailedResult result = _sut.TargetApi(fakeApiAddress, skipSsl);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.CmdResult.ExitCode == 0);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdResult.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdResult.StdErr);
        }

        [TestMethod]
        [TestCategory("TargetApi")]
        public void TargetApi_ReturnsFalse_WhenCmdExitCodeIsNotZero()
        {
            var fakeApiAddress = "my.api.addr";
            bool skipSsl = true;
            string expectedArgs = $"{CfCliService._targetApiCmd} {fakeApiAddress} --skip-ssl-validation";

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null, null))
                .Returns(new CommandResult(_fakeStdOut, _fakeStdErr, 1));

            DetailedResult result = _sut.TargetApi(fakeApiAddress, skipSsl);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.CmdResult.ExitCode == 1);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdResult.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdResult.StdErr);
        }

        [TestMethod]
        [TestCategory("Authenticate")]
        public async Task Authenticate_ReturnsTrue_WhenCmdExitCodeIsZero()
        {
            var fakeUsername = "uname";
            var fakePw = new SecureString();
            var fakeDecodedPw = "";
            string expectedArgs = $"{CfCliService._authenticateCmd} {fakeUsername} {fakeDecodedPw}";

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null, null))
                .Returns(new CommandResult(_fakeStdOut, _fakeStdErr, 0));

            DetailedResult result = await _sut.AuthenticateAsync(fakeUsername, fakePw);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.CmdResult.ExitCode == 0);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdResult.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdResult.StdErr);
        }

        [TestMethod]
        [TestCategory("Authenticate")]
        public async Task Authenticate_ReturnsFalse_WhenCmdExitCodeIsNotZero()
        {
            var fakeUsername = "uname";
            var fakePw = new SecureString();
            var fakeDecodedPw = "";
            string expectedArgs = $"{CfCliService._authenticateCmd} {fakeUsername} {fakeDecodedPw}";

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null, null))
                .Returns(new CommandResult(_fakeStdOut, _fakeStdErr, 1));

            DetailedResult result = await _sut.AuthenticateAsync(fakeUsername, fakePw);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.CmdResult.ExitCode == 1);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdResult.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdResult.StdErr);
        }

        [TestMethod]
        [TestCategory("TargetOrg")]
        public void TargetOrg_ReturnsTrue_WhenCmdExitCodeIsZero()
        {
            var fakeOrgName = "fake org";
            string expectedArgs = $"{CfCliService._targetOrgCmd} \"{fakeOrgName}\""; // expect org name to be surrounded by double quotes
            CommandResult fakeSuccessResult = new CommandResult(_fakeStdOut, _fakeStdErr, 0);

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null, null))
                .Returns(fakeSuccessResult);

            var result = _sut.TargetOrg(fakeOrgName);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.CmdResult.ExitCode == 0);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdResult.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdResult.StdErr);
        }

        [TestMethod]
        [TestCategory("TargetOrg")]
        public void TargetOrg_ReturnsFalse_WhenCmdExitCodeIsNotZero()
        {
            var fakeOrgName = "fake org";
            string expectedArgs = $"{CfCliService._targetOrgCmd} \"{fakeOrgName}\""; // expect org name to be surrounded by double quotes
            CommandResult fakeFailureResult = new CommandResult(_fakeStdOut, _fakeStdErr, 1);

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null, null))
                .Returns(fakeFailureResult);

            var result = _sut.TargetOrg(fakeOrgName);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.CmdResult.ExitCode == 1);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdResult.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdResult.StdErr);
        }

        [TestMethod]
        [TestCategory("TargetOrg")]
        public void TargetOrg_ThrowsInvalidRefreshTokenException_WhenStdErrReportsInvalidToken()
        {
            var fakeOrgName = "fake org";
            string expectedArgs = $"{CfCliService._targetOrgCmd} \"{fakeOrgName}\""; // expect org name to be surrounded by double quotes
            var tokenFailureResult = new CommandResult("junk", CfCliService._invalidRefreshTokenError, 1);

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null, null))
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
            CommandResult fakeSuccessResult = new CommandResult(_fakeStdOut, _fakeStdErr, 0);

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null, null))
                .Returns(fakeSuccessResult);

            var result = _sut.TargetSpace(fakeSpaceName);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.CmdResult.ExitCode == 0);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdResult.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdResult.StdErr);
        }

        [TestMethod]
        [TestCategory("TargetSpace")]
        public void TargetSpace_ReturnsFalseResult_WhenCmdExitCodeIsNotZero()
        {
            var fakeSpaceName = "fake-space";
            string expectedArgs = $"{CfCliService._targetSpaceCmd} \"{fakeSpaceName}\""; // ensure space name gets surrounded by quotes
            CommandResult fakeFailureResult = new CommandResult(_fakeStdOut, _fakeStdErr, 1);

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null, null))
                .Returns(fakeFailureResult);

            var result = _sut.TargetSpace(fakeSpaceName);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.CmdResult.ExitCode == 1);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdResult.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdResult.StdErr);
        }

        [TestMethod]
        [TestCategory("TargetSpace")]
        public void TargetSpace_ThrowsInvalidRefreshTokenException_WhenStdErrReportsInvalidToken()
        {
            var fakeSpaceName = "fake-space";
            string expectedArgs = $"{CfCliService._targetSpaceCmd} \"{fakeSpaceName}\""; // ensure space name gets surrounded by quotes
            var tokenFailureResult = new CommandResult("junk", CfCliService._invalidRefreshTokenError, 1);

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null, null))
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
            CommandResult fakeSuccessResult = new CommandResult(_fakeStdOut, _fakeStdErr, 0);

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null, null))
                .Returns(fakeSuccessResult);

            DetailedResult result = await _sut.StopAppByNameAsync(fakeAppName);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.CmdResult.ExitCode == 0);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdResult.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdResult.StdErr);
        }

        [TestMethod]
        [TestCategory("StopApp")]
        public async Task StopAppByNameAsync_ReturnsFalseResult_WhenCmdExitCodeIsNotZero()
        {
            var fakeAppName = "fake app name with spaces";
            string expectedArgs = $"{CfCliService._stopAppCmd} \"{fakeAppName}\""; // expect app name to be surrounded by double quotes
            CommandResult fakeFailureResult = new CommandResult(_fakeStdOut, _fakeStdErr, 1);

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null, null))
                .Returns(fakeFailureResult);

            DetailedResult result = await _sut.StopAppByNameAsync(fakeAppName);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.CmdResult.ExitCode == 1);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdResult.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdResult.StdErr);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppByNameAsync_ReturnsTrueResult_WhenCmdExitCodeIsZero()
        {
            var fakeAppName = "fake app name with spaces";
            string expectedCmdStr = $"{CfCliService._startAppCmd} \"{fakeAppName}\""; // expect app name to be surrounded by double quotes
            CommandResult fakeSuccessResult = new CommandResult(_fakeStdOut, _fakeStdErr, 0);

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedCmdStr, null, _defaultEnvVars, null, null, null))
                .Returns(fakeSuccessResult);

            DetailedResult result = await _sut.StartAppByNameAsync(fakeAppName);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.CmdResult.ExitCode == 0);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdResult.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdResult.StdErr);
        }

        [TestMethod]
        [TestCategory("StartApp")]
        public async Task StartAppByNameAsync_ReturnsFalseResult_WhenCmdExitCodeIsNotZero()
        {
            var fakeAppName = "fake app name with spaces";
            string expectedCmdStr = $"{CfCliService._startAppCmd} \"{fakeAppName}\""; // expect app name to be surrounded by double quotes
            CommandResult fakeFailureResult = new CommandResult(_fakeStdOut, _fakeStdErr, 1);

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedCmdStr, null, _defaultEnvVars, null, null, null))
                .Returns(fakeFailureResult);

            DetailedResult result = await _sut.StartAppByNameAsync(fakeAppName);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.CmdResult.ExitCode == 1);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdResult.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdResult.StdErr);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppByNameAsync_ReturnsTrueResult_WhenCmdExitCodeIsZero()
        {
            var fakeAppName = "fake-app";
            string expectedCmdStr = $"{CfCliService._deleteAppCmd} \"{fakeAppName}\" -r"; // expect app name to be surrounded by double quotes
            CommandResult fakeSuccessResult = new CommandResult(_fakeStdOut, _fakeStdErr, 0);

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedCmdStr, null, _defaultEnvVars, null, null, null))
                .Returns(fakeSuccessResult);

            DetailedResult result = await _sut.DeleteAppByNameAsync(fakeAppName);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.CmdResult.ExitCode == 0);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdResult.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdResult.StdErr);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppByNameAsync_ReturnsFalseResult_WhenCmdExitCodeIsNotZero()
        {
            var fakeAppName = "fake-app";
            string expectedCmdStr = $"{CfCliService._deleteAppCmd} \"{fakeAppName}\" -r"; // expect app name to be surrounded by double quotes
            CommandResult fakeFailureResult = new CommandResult(_fakeStdOut, _fakeStdErr, 1);

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedCmdStr, null, _defaultEnvVars, null, null, null))
                .Returns(fakeFailureResult);

            DetailedResult result = await _sut.DeleteAppByNameAsync(fakeAppName);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.CmdResult.ExitCode == 1);
            Assert.IsNotNull(result.Explanation);
            Assert.AreEqual(_fakeStdOut, result.CmdResult.StdOut);
            Assert.AreEqual(_fakeStdErr, result.CmdResult.StdErr);
        }

        [TestMethod]
        [TestCategory("DeleteApp")]
        public async Task DeleteAppByNameAsync_DoesNotIncludeDashRFlag_WhenRemoveMappedRoutesIsFalse()
        {
            var fakeAppName = "fake-app";
            string expectedCmdStr = $"{CfCliService._deleteAppCmd} \"{fakeAppName}\""; // expect app name to be surrounded by double quotes
            CommandResult fakeSuccessResult = new CommandResult(_fakeStdOut, _fakeStdErr, 0);

            _mockCommandProcessService.Setup(mock => mock.
              RunExecutable(_fakePathToCfExe, expectedCmdStr, null, _defaultEnvVars, null, null, null))
                .Returns(fakeSuccessResult);

            DetailedResult result = await _sut.DeleteAppByNameAsync(fakeAppName, removeMappedRoutes: false);

            _mockCommandProcessService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ReturnsSuccessResult_WhenCommandSucceeds()
        {
            string expectedArgs = $"push -f \"{_fakeManifestPath}\""; // ensure manifest path gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, _fakeProjectPath, _defaultEnvVars, _fakeOutCallback, _fakeErrCallback, null))
                    .Returns(_fakeSuccessCmdResult);

            _mockFileService.Setup(m => m.FileExists(_fakeManifestPath)).Returns(true);

            var result = await _sut.PushAppAsync(_fakeManifestPath, _fakeProjectPath, FakeOrg.OrgName, FakeSpace.SpaceName, _fakeOutCallback, _fakeErrCallback);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(_fakeSuccessCmdResult, result.CmdResult);
        }

        [TestCategory("PushApp")]
        public async Task PushAppAsync_ReturnsFailureResult_WhenCfExeCannotBeFound()
        {
            _mockFileService.SetupGet(m => m.FullPathToCfExe).Returns((string)null);

            var result = await _sut.PushAppAsync(_fakeManifestPath, _fakeProjectPath, FakeOrg.OrgName, FakeSpace.SpaceName, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual("Unable to locate cf.exe.", result.Explanation);
        }

        [TestCategory("PushApp")]
        public async Task PushAppAsync_ReturnsFailureResult_WhenManifestCannotBeFound()
        {
            _mockFileService.Setup(m => m.FileExists(_fakeManifestPath)).Returns(false);

            var result = await _sut.PushAppAsync(_fakeManifestPath, _fakeProjectPath, FakeOrg.OrgName, FakeSpace.SpaceName, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.CmdResult);
            Assert.AreEqual($"Unable to deploy app; no manifest file found at '{_fakeManifestPath}'", result.Explanation);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ReturnsFailureResult_WhenTargetOrgFails()
        {
            string expectedArgs = $"push -f \"{_fakeManifestPath}\""; // ensure manifest path gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes

            _mockFileService.Setup(m => m.FileExists(_fakeManifestPath)).Returns(true);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeFailureCmdResult);

            var result = await _sut.PushAppAsync(_fakeManifestPath, _fakeProjectPath, FakeOrg.OrgName, FakeSpace.SpaceName, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains("Unable to deploy app from"));
            Assert.IsTrue(result.Explanation.Contains(_fakeManifestPath));
            Assert.IsTrue(result.Explanation.Contains("failed to target org"));
            Assert.IsTrue(result.Explanation.Contains(FakeOrg.OrgName));
            Assert.AreEqual(_fakeFailureCmdResult, result.CmdResult);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ReturnsFailureResult_WhenTargetSpaceFails()
        {
            string expectedArgs = $"push -f \"{_fakeManifestPath}\""; // ensure manifest path gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes

            _mockFileService.Setup(m => m.FileExists(_fakeManifestPath)).Returns(true);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeFailureCmdResult);

            var result = await _sut.PushAppAsync(_fakeManifestPath, _fakeProjectPath, FakeOrg.OrgName, FakeSpace.SpaceName, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Explanation.Contains("Unable to deploy app from"));
            Assert.IsTrue(result.Explanation.Contains(_fakeManifestPath));
            Assert.IsTrue(result.Explanation.Contains("failed to target space"));
            Assert.IsTrue(result.Explanation.Contains(FakeSpace.SpaceName));
            Assert.AreEqual(_fakeFailureCmdResult, result.CmdResult);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ReturnsFailureResult_WhenCfCmdFails()
        {
            string expectedArgs = $"push -f \"{_fakeManifestPath}\""; // ensure manifest path gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes

            _mockFileService.Setup(m => m.FileExists(_fakeManifestPath)).Returns(true);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, _fakeProjectPath, _defaultEnvVars, _fakeOutCallback, _fakeErrCallback, null))
                    .Returns(_fakeFailureCmdResult);

            var result = await _sut.PushAppAsync(_fakeManifestPath, _fakeProjectPath, FakeOrg.OrgName, FakeSpace.SpaceName, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(_fakeFailureCmdResult.StdErr, result.Explanation);
            Assert.AreEqual(_fakeFailureCmdResult, result.CmdResult);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ReturnsGenericErrorMessage_WhenCfCmdFailsWithoutStdErr()
        {
            string expectedArgs = $"push -f \"{_fakeManifestPath}\""; // ensure manifest path gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes
            CommandResult mockFailedResult = new CommandResult("Something went wrong but there's no StdErr!", "", 1);

            _mockFileService.Setup(m => m.FileExists(_fakeManifestPath)).Returns(true);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, _fakeProjectPath, _defaultEnvVars, _fakeOutCallback, _fakeErrCallback, null))
                    .Returns(mockFailedResult);

            var result = await _sut.PushAppAsync(_fakeManifestPath, _fakeProjectPath, FakeOrg.OrgName, FakeSpace.SpaceName, _fakeOutCallback, _fakeErrCallback);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual($"Unable to execute `cf {expectedArgs}`.", result.Explanation);
            Assert.AreEqual(mockFailedResult, result.CmdResult);
        }

        [TestMethod]
        [TestCategory("PushApp")]
        public async Task PushAppAsync_ThrowsInvalidRefreshTokenException_WhenTargetOrgReportsInvalidRefreshToken()
        {
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var invalidRefreshTokenCmdResult = new CommandResult("junk output", CfCliService._invalidRefreshTokenError, 1);

            _mockFileService.Setup(m => m.FileExists(_fakeManifestPath)).Returns(true);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(invalidRefreshTokenCmdResult);

            Exception thrownException = null;
            try
            {
                await _sut.PushAppAsync(_fakeManifestPath, _fakeProjectPath, FakeOrg.OrgName, FakeSpace.SpaceName, null, null);
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
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes
            var invalidRefreshTokenCmdResult = new CommandResult("junk output", CfCliService._invalidRefreshTokenError, 1);

            _mockFileService.Setup(m => m.FileExists(_fakeManifestPath)).Returns(true);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(invalidRefreshTokenCmdResult);

            Exception thrownException = null;
            try
            {
                await _sut.PushAppAsync(_fakeManifestPath, _fakeProjectPath, FakeOrg.OrgName, FakeSpace.SpaceName, _fakeOutCallback, _fakeErrCallback);
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
            string expectedArgs = $"push -f \"{_fakeManifestPath}\""; // ensure manifest path gets surrounded by quotes
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes
            var invalidRefreshTokenCmdResult = new CommandResult("junk output", CfCliService._invalidRefreshTokenError, 1);

            _mockFileService.Setup(m => m.FileExists(_fakeManifestPath)).Returns(true);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, _fakeProjectPath, _defaultEnvVars, _fakeOutCallback, _fakeErrCallback, null))
                    .Returns(invalidRefreshTokenCmdResult);

            Exception thrownException = null;
            try
            {
                await _sut.PushAppAsync(_fakeManifestPath, _fakeProjectPath, FakeOrg.OrgName, FakeSpace.SpaceName, _fakeOutCallback, _fakeErrCallback);
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

            CommandResult mockCmdResult = new CommandResult(fakeCmdOutput, string.Empty, 0);

            _mockCommandProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null, null))
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

            _mockCommandProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null, null))
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
            CommandResult mockCmdResult = new CommandResult(unparsableContent, string.Empty, 0);

            _mockCommandProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null, null))
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

            CommandResult mockCmdResult = new CommandResult(fakeCmdOutput, string.Empty, 0);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedLogsCmdArgs, null, _defaultEnvVars, null, null, null))
                    .Returns(mockCmdResult);

            var result = await _sut.GetRecentAppLogs(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName);

            Assert.AreEqual(result.Content, fakeCmdOutput);
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
            Assert.AreEqual(mockCmdResult, result.CmdResult);
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
            CommandResult mockCmdResult = new CommandResult(fakeCmdOutput, errorMsg, 1);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
                RunExecutable(_fakePathToCfExe, expectedArgs, null, _defaultEnvVars, null, null, null))
                    .Returns(mockCmdResult);

            var result = await _sut.GetRecentAppLogs(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(result.Content, fakeCmdOutput);
            Assert.AreEqual(mockCmdResult.StdErr, result.Explanation);
            Assert.AreEqual(mockCmdResult, result.CmdResult);
        }

        [TestMethod]
        [TestCategory("GetRecentAppLogs")]
        public async Task GetRecentAppLogs_ReturnsFailureResult_WhenTargetOrgFails()
        {
            var fakeAppName = "junk";
            var expectedArgs = $"logs {fakeAppName} --recent";
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeFailureCmdResult);

            var result = await _sut.GetRecentAppLogs(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(_fakeFailureCmdResult.StdErr, result.Explanation);
            Assert.AreEqual(_fakeFailureCmdResult, result.CmdResult);
        }

        [TestMethod]
        [TestCategory("GetRecentAppLogs")]
        public async Task GetRecentAppLogs_ReturnsFailureResult_WhenTargetSpaceFails()
        {
            var fakeAppName = "junk";
            var expectedArgs = $"logs {fakeAppName} --recent";
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var expectedTargetSpaceCmdArgs = $"{CfCliService._targetSpaceCmd} \"{FakeSpace.SpaceName}\""; // ensure space name gets surrounded by quotes

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeFailureCmdResult);

            var result = await _sut.GetRecentAppLogs(fakeAppName, FakeOrg.OrgName, FakeSpace.SpaceName);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Content);
            Assert.AreEqual(_fakeFailureCmdResult.StdErr, result.Explanation);
            Assert.AreEqual(_fakeFailureCmdResult, result.CmdResult);
        }

        [TestMethod]
        [TestCategory("GetRecentAppLogs")]
        public async Task GetRecentAppLogs_ThrowsInvalidRefreshTokenException_WhenTargetOrgReportsInvalidRefreshToken()
        {
            var fakeAppName = "my fake app";
            var expectedArgs = $"logs {fakeAppName} --recent";
            var expectedTargetOrgCmdArgs = $"{CfCliService._targetOrgCmd} \"{FakeOrg.OrgName}\""; // ensure org name gets surrounded by quotes
            var invalidRefreshTokenCmdResult = new CommandResult("junk output", CfCliService._invalidRefreshTokenError, 1);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null, null))
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
            var invalidRefreshTokenCmdResult = new CommandResult("junk output", CfCliService._invalidRefreshTokenError, 1);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null, null))
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
            var invalidRefreshTokenCmdResult = new CommandResult("junk output", CfCliService._invalidRefreshTokenError, 1);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetOrgCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null, null))
                .Returns(_fakeSuccessCmdResult);

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedTargetSpaceCmdArgs, null, _defaultEnvVars, null, null, null))
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
        [TestCategory("LoginWithSsoPasscode")]
        public async Task LoginWithSsoPasscode_ReturnsSuccessResult_WhenLoginCommandSucceeds()
        {
            const string fakePasscode = "fake sso passcode";
            string expectedArgs = $"login -a \"{_fakeValidTarget}\" --sso-passcode \"{fakePasscode}\"";
            var expectedProcessCancelTriggers = new List<string> { "OK", "Invalid passcode" };

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedArgs, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>(), expectedProcessCancelTriggers))
                .Returns(_fakeSuccessCmdResult);

            var result = await _sut.LoginWithSsoPasscode(_fakeValidTarget, fakePasscode);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Explanation);
        }

        [TestMethod]
        [TestCategory("LoginWithSsoPasscode")]
        public async Task LoginWithSsoPasscode_ReturnsFailureResult_WhenLoginCommandFails()
        {
            const string fakePasscode = "fake sso passcode";
            string expectedArgs = $"login -a \"{_fakeValidTarget}\" --sso-passcode \"{fakePasscode}\"";
            var expectedProcessCancelTriggers = new List<string> { "OK", "Invalid passcode" };

            _mockCommandProcessService.Setup(m => m.
              RunExecutable(_fakePathToCfExe, expectedArgs, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<StdOutDelegate>(), It.IsAny<StdErrDelegate>(), expectedProcessCancelTriggers))
                .Returns(_fakeFailureCmdResult);

            var result = await _sut.LoginWithSsoPasscode(_fakeValidTarget, fakePasscode);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(_fakeFailureCmdResult.StdErr, result.Explanation);
        }
    }
}