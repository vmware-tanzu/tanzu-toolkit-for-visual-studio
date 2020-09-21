using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using CloudFoundry.UAA;
using Moq;
using System.Windows.Input;
using TanzuForVS.CloudFoundryApiClient;

namespace TanzuForVS.UnitTests
{
    [TestClass()]
    public class LoginFormTests
    {
        private LoginForm _sut;
        private TanzuCloudExplorer _mainWindow;
        private RoutedCommand _openLoginFormWindowCommand;
        private readonly Mock<ICfApiClient> _mockCfApiClient = new Mock<ICfApiClient>();

        [TestInitialize]
        public void TestInit()
        {
            _mainWindow = new TanzuCloudExplorer(_mockCfApiClient.Object);
            _openLoginFormWindowCommand = (RoutedCommand)_mainWindow.Resources["OpenLoginWindow"];
            _openLoginFormWindowCommand.Execute(null, _mainWindow);
            _sut = _mainWindow.LoginForm;
        }

        [TestMethod()]
        public async Task ConnectToCFAsync_SetDataContextError_WhenTargetUriEmpty() 
        {

            string target = "";
            string username = "doesn't matter";
            string password = "doesn't matter";
            string httpProxy = "doesn't matter";
            bool skipSsl = true;
            const string expectedErrorMessage = "Empty target URI";

            Assert.IsFalse(_sut.WindowDataContext.HasErrors, "DataContext should have no errors to start");

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
            const string expectedErrorMessage = "Invalid target URI";

            await _sut.ConnectToCFAsync(target, username, password, httpProxy, skipSsl);

            Assert.IsTrue(_sut.WindowDataContext.HasErrors);
            Assert.AreEqual(expectedErrorMessage, _sut.WindowDataContext.ErrorMessage);
        }

        [TestMethod()]
        public async Task ConnectToCFAsync_SetsDataContextError_WhenLoginThrowsException()
        {
            Assert.IsFalse(_sut.WindowDataContext.HasErrors);

            string targetStr = "http://not.real.uri";
            string username = "doesn't matter";
            string password = "doesn't matter";
            string httpProxy = "doesn't matter";
            bool skipSsl = true;

            const string fakeClientCreationErrorMessage =
                "(Fake message) couldn't login";

            _mockCfApiClient.Setup(x => x.LoginAsync(targetStr, username, password))
                .Throws(new Exception(fakeClientCreationErrorMessage));

            await _sut.ConnectToCFAsync(targetStr, username, password, httpProxy, skipSsl);

            Assert.IsTrue(_sut.WindowDataContext.HasErrors);
            Assert.AreEqual(fakeClientCreationErrorMessage, _sut.WindowDataContext.ErrorMessage);
            _mockCfApiClient.VerifyAll();
        }

        [TestMethod()]
        public async Task ConnectToCFAsync_DisplaysLoginSuccessMessage_WhenLogInSucceeds()
        {
            Assert.IsFalse(_sut.WindowDataContext.IsLoggedIn, "DataContext should not be logged in to start");

            string targetStr = "http://properly.formatted.uri";
            string httpProxy = "doesn't matter";
            string username = "valid username";
            string password = "valid password";
            bool skipSsl = true;
            Uri targetUri = new Uri(targetStr);
            string fakeAccessToken = "thisisafaketoken";

            _mockCfApiClient.Setup(mock => mock.LoginAsync(targetStr, username, password)).ReturnsAsync(fakeAccessToken);

            await _sut.ConnectToCFAsync(targetStr, username, password, httpProxy, skipSsl);

            Assert.IsTrue(_sut.WindowDataContext.IsLoggedIn); // proxy for checking display of login success message 
            _mockCfApiClient.VerifyAll();
        }
        
        [TestMethod()]
        public async Task ConnectToCFAsync_SetsDataContextError_WhenLoginFails()
        {
            Assert.IsFalse(_sut.WindowDataContext.IsLoggedIn, "DataContext should not be logged in to start");

            string targetStr = "http://properly.formatted.uri";
            string httpProxy = "doesn't matter";
            string username = "invalid username";
            string password = "invalid password";
            bool skipSsl = true;

            _mockCfApiClient.Setup(mock => mock.LoginAsync(targetStr, username, password)).ReturnsAsync((string)null);

            await _sut.ConnectToCFAsync(targetStr, username, password, httpProxy, skipSsl);

            Assert.IsFalse(_sut.WindowDataContext.IsLoggedIn);
            Assert.IsTrue(_sut.WindowDataContext.HasErrors);
            Assert.AreEqual("failed to login", _sut.WindowDataContext.ErrorMessage);
            _mockCfApiClient.VerifyAll();
        }
    }
}