using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.CommandProcess;
using Tanzu.Toolkit.Services.DataPersistence;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.File;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.Services.Threading;
using Tanzu.Toolkit.Services.ViewLocator;
using Tanzu.Toolkit.ViewModels.SsoDialog;
using Tanzu.Toolkit.ViewModels.AppDeletionConfirmation;
using static Tanzu.Toolkit.Services.OutputHandler.OutputHandler;

namespace Tanzu.Toolkit.ViewModels.Tests
{
    public abstract class ViewModelTestSupport
    {
        protected IServiceProvider Services { get; set; }

        protected Mock<ICloudFoundryService> MockCloudFoundryService { get; set; }
        protected Mock<IDialogService> MockDialogService { get; set; }
        protected Mock<IErrorDialog> MockErrorDialogService { get; set; }
        protected Mock<IViewLocatorService> MockViewLocatorService { get; set; }
        protected Mock<ILoggingService> MockLoggingService { get; set; }
        protected Mock<ILogger> MockLogger { get; set; }
        protected Mock<IThreadingService> MockThreadingService { get; set; }
        protected Mock<IUiDispatcherService> MockUiDispatcherService { get; set; }
        protected Mock<IFileService> MockFileService { get; set; }
        protected Mock<ITasExplorerViewModel> MockTasExplorerViewModel { get; set; }
        protected Mock<ISerializationService> MockSerializationService { get; set; }
        protected Mock<IDataPersistenceService> MockDataPersistenceService { get; set; }
        protected Mock<ISsoDialogViewModel> MockSsoViewModel { get; set; }
        protected Mock<ILoginViewModel> MockLoginViewModel { get; set; }
        protected Mock<IAppDeletionConfirmationViewModel> MockAppDeletionConfirmationViewModel { get; set; }


        protected const string FakeCfName = "fake cf name";
        protected const string FakeCfApiAddress = "http://fake.api.address";
        protected const string FakeAccessToken = "fake.access.token";
        protected const string FakeOrgName = "fake org name";
        protected const string FakeOrgGuid = "fake-org-guid";
        protected const string FakeSpaceName = "fake space name";
        protected const string FakeSpaceGuid = "fake-space-guid";

        protected static readonly CloudFoundryInstance FakeCfInstance = new CloudFoundryInstance(FakeCfName, FakeCfApiAddress);
        protected static readonly CloudFoundryOrganization FakeCfOrg = new CloudFoundryOrganization(FakeOrgName, FakeOrgGuid, FakeCfInstance);
        protected static readonly CloudFoundrySpace FakeCfSpace = new CloudFoundrySpace(FakeSpaceName, FakeSpaceGuid, FakeCfOrg);

        protected static readonly List<CloudFoundryOrganization> EmptyListOfOrgs = new List<CloudFoundryOrganization>();
        protected static readonly List<CloudFoundrySpace> EmptyListOfSpaces = new List<CloudFoundrySpace>();
        protected static readonly List<CloudFoundryApp> EmptyListOfApps = new List<CloudFoundryApp>();

        protected static readonly CommandResult FakeSuccessCmdResult = new CommandResult("junk output", "junk error", 0);
        protected static readonly CommandResult FakeFailureCmdResult = new CommandResult("junk output", "junk error", 1);

        protected static readonly DetailedResult FakeSuccessDetailedResult = new DetailedResult(true, null, FakeSuccessCmdResult);
        protected static readonly DetailedResult FakeFailureDetailedResult = new DetailedResult(false, "junk error", FakeFailureCmdResult);

        protected static readonly string _fakeProjectPath = "this\\is\\a\\fake\\path\\to\\a\\project\\directory";
        protected static readonly string _fakeManifestPath = "this\\is\\a\\fake\\path\\to\\a\\manifest";
        protected static readonly StdOutDelegate _fakeOutCallback = content => { };
        protected static readonly StdErrDelegate _fakeErrCallback = content => { };

        internal string[] sampleManifestLines = File.ReadAllLines("TestFakes//fake-manifest.yml");
        internal string[] sampleInvalidManifestLines = File.ReadAllLines("TestFakes//fake-invalid-manifest.yml");
        internal string[] multiBuildpackManifestLines = File.ReadAllLines("TestFakes//fake-multi-buildpack-manifest.yml");

        protected AppManifest _fakeManifestModel = new AppManifest
        {
            Version = 1,
            Applications = new List<AppConfig>
                {
                    new AppConfig
                    {
                        Name = "app1",
                        Buildpacks = new List<string>
                        {
                            "ruby_buildpack",
                            "java_buildpack",
                            "my_cool_buildpack",
                        },
                        Env = new Dictionary<string, string>
                        {
                            {"VAR1", "value1" },
                            {"VAR2", "value2" },
                        },
                        Routes = new List<RouteConfig>
                        {
                            new RouteConfig
                            {
                                Route = "route.example.com",
                            },
                            new RouteConfig
                            {
                                Route = "another-route.example.com",
                                Protocol = "http2"
                            },
                        },
                        Services = new List<string> {
                            "my-service1",
                            "my-service2",
                        },
                        Stack = "cflinuxfs3",
                        Metadata = new MetadataConfig
                        {
                            Annotations = new Dictionary<string, string>
                            {
                                { "contact", "bob@example.com jane@example.com" },
                            },
                            Labels = new Dictionary<string, string>
                            {
                                { "sensitive", "true" },
                            },
                        },
                        Processes = new List<ProcessConfig>
                        {
                            new ProcessConfig
                            {
                                Type = "web",
                                Command = "start-web.sh",
                                DiskQuota = "512M",
                                HealthCheckHttpEndpoint = "/healthcheck",
                                HealthCheckType = "http",
                                HealthCheckInvocationTimeout = 10,
                                Instances = 3,
                                Memory = "500M",
                                Timeout = 10,
                            },
                            new ProcessConfig
                            {
                                Type = "worker",
                                Command = "start-worker.sh",
                                DiskQuota = "1G",
                                HealthCheckType = "process",
                                Instances = 2,
                                Memory = "256M",
                                Timeout = 15,
                            },
                        },
                        Path = "some//fake//path",
                    },
                    new AppConfig
                    {
                        Name = "app2",
                        Env = new Dictionary<string, string>
                        {
                            { "VAR1", "value1" },
                        },
                        Processes = new List<ProcessConfig>
                        {
                            new ProcessConfig
                            {
                                Type = "web",
                                Instances = 1,
                                Memory = "256M",
                            },
                        },
                        Sidecars = new List<SidecarConfig>
                        {
                            new SidecarConfig
                            {
                                Name = "authenticator",
                                ProcessTypes = new List<string>
                                {
                                    "web",
                                    "worker",
                                },
                                Command = "bundle exec run-authenticator",
                                Memory = "800M",
                            },
                            new SidecarConfig
                            {
                                Name = "upcaser",
                                ProcessTypes = new List<string>
                                {
                                    "worker",
                                },
                                Command = "./tr-server",
                                Memory = "2G",
                            }
                        }
                    }
                }
        };

        protected ViewModelTestSupport()
        {
            RenewMockServices();
        }

        protected void RenewMockServices()
        {
            var services = new ServiceCollection();

            MockCloudFoundryService = new Mock<ICloudFoundryService>();
            MockErrorDialogService = new Mock<IErrorDialog>();
            MockDialogService = new Mock<IDialogService>();
            MockViewLocatorService = new Mock<IViewLocatorService>();
            MockLoggingService = new Mock<ILoggingService>();
            MockThreadingService = new Mock<IThreadingService>();
            MockUiDispatcherService = new Mock<IUiDispatcherService>();
            MockFileService = new Mock<IFileService>();
            MockTasExplorerViewModel = new Mock<ITasExplorerViewModel>();
            MockSerializationService = new Mock<ISerializationService>();
            MockDataPersistenceService = new Mock<IDataPersistenceService>();
            MockSsoViewModel = new Mock<ISsoDialogViewModel>();
            MockLoginViewModel = new Mock<ILoginViewModel>();
            MockAppDeletionConfirmationViewModel = new Mock<IAppDeletionConfirmationViewModel>();

            MockLogger = new Mock<ILogger>();
            MockLoggingService.SetupGet(m => m.Logger).Returns(MockLogger.Object);

            services.AddSingleton(MockCloudFoundryService.Object);
            services.AddSingleton(MockErrorDialogService.Object);
            services.AddSingleton(MockDialogService.Object);
            services.AddSingleton(MockViewLocatorService.Object);
            services.AddSingleton(MockLoggingService.Object);
            services.AddSingleton(MockThreadingService.Object);
            services.AddSingleton(MockUiDispatcherService.Object);
            services.AddSingleton(MockTasExplorerViewModel.Object);
            services.AddSingleton(MockFileService.Object);
            services.AddSingleton(MockSerializationService.Object);
            services.AddSingleton(MockDataPersistenceService.Object);
            services.AddSingleton(MockSsoViewModel.Object);
            services.AddSingleton(MockLoginViewModel.Object);
            services.AddSingleton(MockAppDeletionConfirmationViewModel.Object);

            Services = services.BuildServiceProvider();
        }
    }
}
