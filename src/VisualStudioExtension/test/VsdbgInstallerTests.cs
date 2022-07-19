﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.CfCli;
using Tanzu.Toolkit.Services.CommandProcess;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.VisualStudio.Services;

namespace Tanzu.Toolkit.VisualStudioExtension.Tests
{
    [TestClass]
    public class VsdbgInstallerTests
    {
        private const string _emptyStr = "";
        private VsdbgInstaller _sut;
        private Mock<ICfCliService> _mockCfCliService;
        private static readonly CloudFoundryOrganization _fakeOrg = new CloudFoundryOrganization("fake org", "fake org guid", null);
        private static readonly CloudFoundrySpace _fakeSpace = new CloudFoundrySpace("fake space", "fake space guid", _fakeOrg);
        private static readonly CloudFoundryApp _fakeLinuxApp = new CloudFoundryApp("fake app", "fake app guid", _fakeSpace, null)
        {
            Stack = "linux",
        };
        private static readonly CloudFoundryApp _fakeWindowsApp = new CloudFoundryApp("fake app", "fake app guid", _fakeSpace, null)
        {
            Stack = "windows",
        };

        [TestInitialize]
        public void TestInit()
        {
            _mockCfCliService = new Mock<ICfCliService>();

            _sut = new VsdbgInstaller(_mockCfCliService.Object);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(_emptyStr)]
        [DataRow("invalid stack")]
        public async Task InstallVsdbgForCFAppAsync_ReturnsFailedResult_WhenAppStackInvalid(string stack)
        {
            var appWithInvalidStack = new CloudFoundryApp("fake app", "fake app guid", _fakeSpace, null)
            {
                Stack = stack,
            };

            var result = await _sut.InstallVsdbgForCFAppAsync(appWithInvalidStack, "junk");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual($"Unexpected stack: '{stack}'", result.Explanation);
            _mockCfCliService.Verify(m => m.ExecuteSshCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        [DataRow(null, "linux")]
        [DataRow(null, "windows")]
        [DataRow(_emptyStr, "linux")]
        [DataRow(_emptyStr, "windows")]
        public async Task InstallVsdbgForCFAppAsync_UsesLatest_WhenVSVersionMissing(string vsVersion, string stack)
        {
            var app = new CloudFoundryApp("fake app", "fake app guid", _fakeSpace, null)
            {
                Stack = stack,
            };

            var result = await _sut.InstallVsdbgForCFAppAsync(app, vsVersion);

            var expectedVSVersion = "latest";
            _mockCfCliService.Verify(m => m.ExecuteSshCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.Is<string>(s => s.Contains(expectedVSVersion))), Times.Once);
        }

        [TestMethod]
        [DataRow("linux")]
        [DataRow("windows")]
        public async Task InstallVsdbgForCFAppAsync_ReturnsFailedResult_WhenSshCmdFails(string stack)
        {
            var app = new CloudFoundryApp("fake app", "fake app guid", _fakeSpace, null)
            {
                Stack = stack,
            };
            var sshFailure = new DetailedResult(false, "failed because we told it to!", new CommandResult("some output", "some error", 1));

            _mockCfCliService.Setup(m => m.ExecuteSshCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(sshFailure);

            var result = await _sut.InstallVsdbgForCFAppAsync(app, "junk");

            Assert.AreEqual(sshFailure.Succeeded, result.Succeeded);
            Assert.AreEqual(sshFailure.Explanation, result.Explanation);
            Assert.AreEqual(sshFailure.CmdResult, result.CmdResult);
        }

        [TestMethod]
        [DataRow("linux")]
        [DataRow("windows")]
        public async Task InstallVsdbgForCFAppAsync_UsesOSSpecificValuesForVsdbgInstallation_BasedOnStack(string stack)
        {
            var app = new CloudFoundryApp("fake app", "fake app guid", _fakeSpace, null)
            {
                Stack = stack,
            };
            var vsVersion = "junk";
            var expectedStartCmd = stack == "windows" 
                ? $"powershell -File {VsdbgInstaller._vsdbgInstallScriptName}.ps1 -Version {vsVersion} -RuntimeID win7-x64 -InstallPath .\\{_sut.VsdbgDirName}" 
                : $"chmod +x {VsdbgInstaller._vsdbgInstallScriptName}.sh && ./{VsdbgInstaller._vsdbgInstallScriptName}.sh -v {vsVersion} -r linux-x64 -l ./{_sut.VsdbgDirName}";
            var expectedSshCmd = $"cd app && curl -L https://aka.ms/getvsdbg{(stack == "windows" ? "ps1" : "sh")} -o GetVsDbg.{(stack == "windows" ? "ps1" : "sh")} && {expectedStartCmd}";

            var result = await _sut.InstallVsdbgForCFAppAsync(app, vsVersion);

            _mockCfCliService.Verify(m => m.ExecuteSshCommand(app.AppName, app.ParentSpace.ParentOrg.OrgName, app.ParentSpace.SpaceName, expectedSshCmd), Times.Once);
        }
    }
}
