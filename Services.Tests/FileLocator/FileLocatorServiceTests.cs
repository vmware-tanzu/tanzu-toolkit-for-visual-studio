using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using Tanzu.Toolkit.VisualStudio.Services.FileLocator;

namespace Tanzu.Toolkit.VisualStudio.Services.Tests.FileLocator
{
    [TestClass()]
    public class FileLocatorServiceTests : ServicesTestSupport
    {
        private FileLocatorService sut;

        [TestInitialize]
        public void TestInit()
        {
            var fakeAssemblyBaseDir = Path.GetDirectoryName(GetType().Assembly.Location);
            sut = new FileLocatorService(fakeAssemblyBaseDir);
        }

        [TestMethod]
        public void CliVersion_Is7_ByDefault()
        {
            Assert.AreEqual(7, sut.CliVersion);
        }

        [TestMethod]
        public void CliVersion_CanBeSetTo6()
        {
            Assert.AreEqual(7, sut.CliVersion);

            sut.CliVersion = 6;
            Assert.AreEqual(6, sut.CliVersion);
        }

        [TestMethod]
        public void CliVersion_CanBeSetTo7()
        {
            sut.CliVersion = 6;
            Assert.AreEqual(6, sut.CliVersion);

            sut.CliVersion = 7;
            Assert.AreEqual(7, sut.CliVersion);
        }

        [TestMethod]
        public void CliVersion_CannotBeSetToAnythingBut6Or7()
        {
            Assert.AreEqual(7, sut.CliVersion);

            sut.CliVersion = 5;

            Assert.AreEqual(7, sut.CliVersion);
        }

        [TestMethod]
        public void FullPathToCfExe_ReturnsV6Path_WhenCliVersionIs6()
        {

            sut.CliVersion = 6;

            var path = sut.FullPathToCfExe;

            Assert.IsTrue(path.Contains("cf6.exe"));
        }

        [TestMethod]
        public void FullPathToCfExe_ReturnsV7Path_WhenCliVersionIs7()
        {

            sut.CliVersion = 7;

            var path = sut.FullPathToCfExe;

            Assert.IsTrue(path.Contains("cf7.exe"));
        }

        [TestMethod]
        public void FullPathToCfExe_ThrowsException_WhenExecutableFileNotFound()
        {
            var fakeAssemblyBaseDir = "/fake/path";
            sut = new FileLocatorService(fakeAssemblyBaseDir)
            {
                CliVersion = 7
            };

            Exception expectedException = null;

            try
            {
                var path = sut.FullPathToCfExe;
            }
            catch (Exception ex)
            {
                expectedException = ex;
            }

            Assert.IsNotNull(expectedException);
            Assert.IsTrue(expectedException.Message.Contains(sut.CliVersion.ToString()));
        }

    }
}
