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
        private Type _fakeViewType;
        private readonly FakeView _fakeReturnedView = new();

        [TestInitialize]
        public void TestInit()
        {
            _services = new FakeServiceProvider(_fakeReturnedView);

            _mockToolWindowService = new Mock<IToolWindowService>();
            _mockLoggingService = new Mock<ILoggingService>();

            _mockLogger = new Mock<ILogger>();
            _mockLoggingService.SetupGet(m => m.Logger).Returns(_mockLogger.Object);

            _sut = new TestVsViewLocatorService(_mockToolWindowService.Object, _mockLoggingService.Object, _services);

            _fakeViewType = GetType(); // some arbitrary type to return

            // pretend that looking up `expectedViewName` returns `fakeViewType`
            _sut.TypeLookupOverrides = new Dictionary<string, Type>
            {
                { _fakeViewName, _fakeViewType }
            };
        }

        [TestMethod]
        [TestCategory("GetViewByViewModelName")]
        public void GetViewByViewModelName_ReturnsView_OfTypeMatchingProvidedViewModel()
        {
            var result = _sut.GetViewByViewModelName(_fakeViewModelName);

            Assert.AreEqual(_fakeReturnedView, result);

            var fakeServiceProvider = _services as FakeServiceProvider;
            Assert.IsTrue(fakeServiceProvider.GetServiceWasCalled);
            Assert.AreEqual(_fakeViewType, fakeServiceProvider.GetServiceArgument);
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

        public Dictionary<string, Type> TypeLookupOverrides { get; set; }

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
