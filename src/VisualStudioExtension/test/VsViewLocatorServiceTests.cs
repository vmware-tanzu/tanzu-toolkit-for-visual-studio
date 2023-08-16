using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.ViewModels;
using Tanzu.Toolkit.VisualStudio.Services;
using Tanzu.Toolkit.VisualStudio.Views;

namespace Tanzu.Toolkit.VisualStudioExtension.Tests
{
    [TestClass]
    public class VsViewLocatorServiceTests
    {
        private TestVsViewLocatorService _sut;

        private IServiceProvider _services;

        private Mock<IToolWindowService> _mockToolWindowService;
        private Mock<ILoggingService> _mockLoggingService;
        private Mock<ILogger> _mockLogger;

        private readonly string _fakeViewModelName = "MyCoolViewModel";
        private readonly string _fakeViewName = "IMyCoolView";
        private readonly FakeView _fakeView = new FakeView();

        [TestInitialize]
        public void TestInit()
        {
            _services = new FakeServiceProvider(_fakeView);

            _mockToolWindowService = new Mock<IToolWindowService>();
            _mockLoggingService = new Mock<ILoggingService>();

            _mockLogger = new Mock<ILogger>();
            _mockLoggingService.SetupGet(m => m.Logger).Returns(_mockLogger.Object);

            _sut = new TestVsViewLocatorService(_mockToolWindowService.Object, _mockLoggingService.Object, _services);
        }

        [TestMethod]
        [TestCategory("GetViewByViewModelName")]
        public void GetViewByViewModelName_ReturnsView_OfTypeMatchingProvidedViewModel_ForToolWindowTypes()
        {
            var dummyToolWindowViewType = typeof(IOutputView);
            Assert.IsTrue(_sut.ViewShownAsToolWindow(dummyToolWindowViewType)); // ensure this type is characterized as a tool window view

            Assert.AreEqual(_sut.GetViewName(_fakeViewModelName), _fakeViewName);
            _sut.TypeLookupOverrides[_fakeViewName] = dummyToolWindowViewType;

            _mockToolWindowService.Setup(m => m.CreateToolWindowForView(dummyToolWindowViewType, It.IsAny<string>())).Returns(_fakeView);

            var result = _sut.GetViewByViewModelName(_fakeViewModelName);

            Assert.AreEqual(_fakeView, result);
            _mockToolWindowService.VerifyAll();
        }

        [TestMethod]
        [TestCategory("GetViewByViewModelName")]
        public void GetViewByViewModelName_ReturnsView_OfTypeMatchingProvidedViewModel_ForModalTypes()
        {
            var dummyToolWindowViewType = typeof(ILoginView);
            Assert.IsTrue(_sut.ViewShownAsModal(dummyToolWindowViewType)); // ensure this type is characterized as a modal view

            Assert.AreEqual(_sut.GetViewName(_fakeViewModelName), _fakeViewName);
            _sut.TypeLookupOverrides[_fakeViewName] = dummyToolWindowViewType;

            var result = _sut.GetViewByViewModelName(_fakeViewModelName);

            Assert.AreEqual(_fakeView, result);

            var fakeServiceProvider = _services as FakeServiceProvider;
            Assert.IsTrue(fakeServiceProvider.GetServiceWasCalled);
            Assert.AreEqual(dummyToolWindowViewType, fakeServiceProvider.GetServiceArgument);
        }

        [TestMethod]
        [TestCategory("GetViewByViewModelName")]
        public void GetViewByViewModelName_ReturnsNull_AndLogsError_WhenTypeNotModalNorToolWindow()
        {
            var dummyToolWindowViewType = GetType(); // some arbitrary type to return
            _sut.TypeLookupOverrides.Add(_fakeViewName, dummyToolWindowViewType);

            Assert.IsFalse(_sut.ViewShownAsToolWindow(dummyToolWindowViewType));
            Assert.IsFalse(_sut.ViewShownAsModal(dummyToolWindowViewType));

            var result = _sut.GetViewByViewModelName(_fakeViewModelName);

            Assert.IsNull(result);
            _mockLogger.Verify(m => m.Error(It.Is<string>(s => s.Contains("given type not classified as either modal or tool window")),
                                            nameof(VsViewLocatorService),
                                            nameof(_sut.GetViewByViewModelName),
                                            _fakeViewModelName), Times.Once);
        }

        [TestMethod]
        [TestCategory("GetViewByViewModelName")]
        public void GetViewByViewModelName_CreatesToolWindow_AndReturnsView_WhenInterpretedViewTypeIsIOutputView()
        {
            var expectedViewType = typeof(IOutputView);
            var fakeViewFromToolWindow = new FakeView();
            var fakeCaptionParam = "some caption";

            _sut.TypeLookupOverrides = new Dictionary<string, Type>
            {
                { _fakeViewName, expectedViewType }
            };

            _mockToolWindowService.Setup(m => m.CreateToolWindowForView(expectedViewType, fakeCaptionParam)).Returns(fakeViewFromToolWindow);

            var result = _sut.GetViewByViewModelName(_fakeViewModelName, fakeCaptionParam);

            Assert.AreEqual(fakeViewFromToolWindow, result);
            _mockToolWindowService.VerifyAll();
        }
    }

    /// <summary>
    /// Using a test class only for the purpose of overriding LookupViewType
    /// </summary>
    internal class TestVsViewLocatorService : VsViewLocatorService
    {
        public TestVsViewLocatorService(IToolWindowService toolWindowService, ILoggingService loggingService, IServiceProvider serviceProvider) : base(toolWindowService, loggingService, serviceProvider)
        {
        }

        public Dictionary<string, Type> TypeLookupOverrides { get; set; } = new Dictionary<string, Type>();

        protected override Type LookupViewType(string viewTypeName)
        {
            var overriddenType = TypeLookupOverrides[viewTypeName];
            return overriddenType ?? base.LookupViewType(viewTypeName);
        }
    }

    class FakeServiceProvider : IServiceProvider
    {
        public FakeServiceProvider(object fakeServiceToReturn)
        {
            GetServiceWasCalled = false;
            FakeServiceToReturn = fakeServiceToReturn;
        }

        public object FakeServiceToReturn { get; set; }

        public bool GetServiceWasCalled { get; private set; }

        public Type GetServiceArgument { get; private set; }

        public object GetService(Type serviceType)
        {
            GetServiceWasCalled = true;
            GetServiceArgument = serviceType;
            return FakeServiceToReturn ?? "Fake return value (were you expecting a real service?)";
        }
    }

    class FakeView : IView
    {
        public IViewModel ViewModel => throw new NotImplementedException();

        public Action DisplayView { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
