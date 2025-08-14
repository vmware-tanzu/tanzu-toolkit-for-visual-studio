using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
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
        public void CliVersion_Is8_ByDefault()
        {
            Assert.AreEqual(8, _sut.CliVersion);
        }

        [TestMethod]
        public void FullPathToCfExe_ReturnsV8Path_WhenCliVersionIs8()
        {
            _sut.CliVersion = 8;
            Assert.Contains("cf8.exe", _sut.FullPathToCfExe);
        }

        [TestMethod]
        public void FullPathToCfExe_ThrowsException_WhenExecutableFileNotFound()
        {
            _sut = new FileService("/fake/path") { CliVersion = 8 };

            Exception expectedException = null;

            try
            {
                _ = _sut.FullPathToCfExe;
            }
            catch (Exception ex)
            {
                expectedException = ex;
            }

            Assert.IsNotNull(expectedException);
            Assert.Contains(_sut.CliVersion.ToString(), expectedException.Message);
        }
    }
}