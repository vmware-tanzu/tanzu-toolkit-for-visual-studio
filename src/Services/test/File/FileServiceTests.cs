using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tanzu.Toolkit.Services.File;

namespace Tanzu.Toolkit.Services.Tests.File
{
    [TestClass]
    public class FileServiceTests : ServicesTestSupport
    {
        private FileService _sut;

        [TestInitialize]
        public void TestInit()
        {
            var fakeAssemblyBaseDir = Path.GetDirectoryName(GetType().Assembly.Location);
            _sut = new FileService(fakeAssemblyBaseDir);
        }

        [TestMethod]
        public void CliVersion_Is7_ByDefault()
        {
            Assert.AreEqual(7, _sut.CliVersion);
        }

        [TestMethod]
        public void CliVersion_CanBeSetTo6()
        {
            Assert.AreEqual(7, _sut.CliVersion);

            _sut.CliVersion = 6;
            Assert.AreEqual(6, _sut.CliVersion);
        }

        [TestMethod]
        public void CliVersion_CanBeSetTo7()
        {
            _sut.CliVersion = 6;
            Assert.AreEqual(6, _sut.CliVersion);

            _sut.CliVersion = 7;
            Assert.AreEqual(7, _sut.CliVersion);
        }

        [TestMethod]
        public void CliVersion_CannotBeSetToAnythingBut6Or7()
        {
            Assert.AreEqual(7, _sut.CliVersion);

            _sut.CliVersion = 5;

            Assert.AreEqual(7, _sut.CliVersion);
        }

        [TestMethod]
        public void FullPathToCfExe_ReturnsV6Path_WhenCliVersionIs6()
        {
            _sut.CliVersion = 6;

            var path = _sut.FullPathToCfExe;

            Assert.IsTrue(path.Contains("cf6.exe"));
        }

        [TestMethod]
        public void FullPathToCfExe_ReturnsV7Path_WhenCliVersionIs7()
        {
            _sut.CliVersion = 7;

            var path = _sut.FullPathToCfExe;

            Assert.IsTrue(path.Contains("cf7.exe"));
        }

        [TestMethod]
        public void FullPathToCfExe_ThrowsException_WhenExecutableFileNotFound()
        {
            var fakeAssemblyBaseDir = "/fake/path";
            _sut = new FileService(fakeAssemblyBaseDir)
            {
                CliVersion = 7,
            };

            Exception expectedException = null;

            try
            {
                var path = _sut.FullPathToCfExe;
            }
            catch (Exception ex)
            {
                expectedException = ex;
            }

            Assert.IsNotNull(expectedException);
            Assert.IsTrue(expectedException.Message.Contains(_sut.CliVersion.ToString()));
        }
    }
}
