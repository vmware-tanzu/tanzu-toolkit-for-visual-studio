using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using CloudFoundry.UAA;
using Moq;

namespace TanzuForVS.UnitTests
{
    [TestClass()]
    public class CFLoginToolWindowControlTests
    {
        private CFLoginToolWindowControl _sut;
        private readonly Mock<ICfApiClientFactory> _mockCfApiClientFactory = new Mock<ICfApiClientFactory>();
        private readonly Mock<IUAA> _mockCfApiClient = new Mock<IUAA>();

        [TestInitialize]
        public void TestInit()
        {
            _sut = new CFLoginToolWindowControl(_mockCfApiClientFactory.Object);
        }


        [TestMethod()]
        public async Task ConnectToCFAsync_SetDataContextError_WhenTargetUriEmpty()
        {
            Assert.IsFalse(_sut.WindowDataContext.HasErrors, "DataContext should have no errors to start");

            string target = "";
            string username = "doesn't matter";
            string password = "doesn't matter";
            string httpProxy = "doesn't matter";
            bool skipSsl = true;
            const string expectedErrorMessage = "Invalid URI: The URI is empty.";

            await _sut.ConnectToCFAsync(target, username, password, httpProxy, skipSsl);

            Assert.IsTrue(_sut.WindowDataContext.HasErrors);
            Assert.AreEqual(expectedErrorMessage, _sut.WindowDataContext.ErrorMessage);
        }


        [TestMethod()]
        public async Task ConnectToCFAsync_SetsDataContextError_WhenTargetUriIsMalformed()
        {
            Assert.IsFalse(_sut.WindowDataContext.HasErrors, "DataContext should have no errors to start");

            string target = "not-properly.formatted.uri";
            string username = "doesn't matter";
            string password = "doesn't matter";
            string httpProxy = "doesn't matter";
            bool skipSsl = true;
            const string expectedErrorMessage = "Invalid URI: The format of the URI could not be determined.";

            await _sut.ConnectToCFAsync(target, username, password, httpProxy, skipSsl);

            Assert.IsTrue(_sut.WindowDataContext.HasErrors);
            Assert.AreEqual(expectedErrorMessage, _sut.WindowDataContext.ErrorMessage);
        }


        [TestMethod()]
        public async Task ConnectToCFAsync_SetsDataContextError_WhenTargetUriIsUnreachable()
        {
            Assert.IsFalse(_sut.WindowDataContext.HasErrors);

            string targetStr = "http://not.real.uri";
            string username = "doesn't matter";
            string password = "doesn't matter";
            string httpProxy = "doesn't matter";
            bool skipSsl = true;
            Uri targetUri = new Uri(targetStr);
            const string fakeClientCreationErrorMessage = 
                "(Fake message) couldn't create client";

            _mockCfApiClientFactory.Setup(x => x.CreateCfApiV2Client(targetUri, null, skipSsl))
                .Throws(new Exception(fakeClientCreationErrorMessage));

            await _sut.ConnectToCFAsync(targetStr, username, password, httpProxy, skipSsl);

            Assert.IsTrue(_sut.WindowDataContext.HasErrors);
            Assert.AreEqual(fakeClientCreationErrorMessage, _sut.WindowDataContext.ErrorMessage);
        }


        [TestMethod()]
        public async Task ConnectToCFAsync_SetsDataContextError_WhenCredentialsAreInvalid()
        {
            Assert.IsFalse(_sut.WindowDataContext.HasErrors, "DataContext should have no errors to start");

            string targetStr = "http://properly.formatted.uri";
            string httpProxy = "doesn't matter";
            string username = "invalid username";
            string password = "invalid password";
            bool skipSsl = true;
            Uri targetUri = new Uri(targetStr);
            const string fakeLoginErrorMessage = "(Fake message) couldn't login";

            _mockCfApiClientFactory.Setup(x => x.CreateCfApiV2Client(targetUri, null, skipSsl))
                .Returns(_mockCfApiClient.Object);

            _mockCfApiClient.Setup(x => x.Login(It.IsAny<CloudCredentials>()))
                .ThrowsAsync(new Exception(fakeLoginErrorMessage));

            await _sut.ConnectToCFAsync(targetStr, username, password, httpProxy, skipSsl);

            Assert.IsTrue(_sut.WindowDataContext.HasErrors);
            Assert.AreEqual(fakeLoginErrorMessage, _sut.WindowDataContext.ErrorMessage);
        }
    }
}