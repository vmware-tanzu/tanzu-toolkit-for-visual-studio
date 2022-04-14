using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services;
using Tanzu.Toolkit.Services.CloudFoundry;
using Tanzu.Toolkit.Services.CommandProcess;
using Tanzu.Toolkit.Services.DataPersistence;
using Tanzu.Toolkit.Services.Dialog;
using Tanzu.Toolkit.Services.DotnetCli;
using Tanzu.Toolkit.Services.ErrorDialog;
using Tanzu.Toolkit.Services.File;
using Tanzu.Toolkit.Services.Logging;
using Tanzu.Toolkit.Services.Threading;
using Tanzu.Toolkit.Services.ViewLocator;
using Tanzu.Toolkit.ViewModels.AppDeletionConfirmation;

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
        protected Mock<IDotnetCliService> MockDotnetCliService { get; set; }
        protected Mock<ILoginViewModel> MockLoginViewModel { get; set; }
        protected Mock<IAppDeletionConfirmationViewModel> MockAppDeletionConfirmationViewModel { get; set; }

        protected const string _fakeCfName = "fake cf name";
        protected const string _fakeCfApiAddress = "http://fake.api.address";
        protected const string _fakeAccessToken = "fake.access.token";
        protected const string _fakeOrgName = "fake org name";
        protected const string _fakeOrgGuid = "fake-org-guid";
        protected const string _fakeSpaceName = "fake space name";
        protected const string _fakeSpaceGuid = "fake-space-guid";
        protected const string _fakeAppName = "fake app name";
        protected const string _fakeAppGuid = "fake-app-guid";

        protected static readonly CloudFoundryInstance _fakeCfInstance = new CloudFoundryInstance(_fakeCfName, _fakeCfApiAddress, false);
        protected static readonly CloudFoundryOrganization _fakeCfOrg = new CloudFoundryOrganization(_fakeOrgName, _fakeOrgGuid, _fakeCfInstance);
        protected static readonly CloudFoundrySpace _fakeCfSpace = new CloudFoundrySpace(_fakeSpaceName, _fakeSpaceGuid, _fakeCfOrg);
        protected static readonly CloudFoundryApp _fakeCfApp = new CloudFoundryApp(_fakeAppName, _fakeAppGuid, _fakeCfSpace, "junk state");

        protected static readonly List<CloudFoundryOrganization> _emptyListOfOrgs = new List<CloudFoundryOrganization>();
        protected static readonly List<CloudFoundrySpace> _emptyListOfSpaces = new List<CloudFoundrySpace>();
        protected static readonly List<CloudFoundryApp> _emptyListOfApps = new List<CloudFoundryApp>();

        protected static readonly CommandResult _fakeSuccessCmdResult = new CommandResult("junk output", "junk error", 0);
        protected static readonly CommandResult _fakeFailureCmdResult = new CommandResult("junk output", "junk error", 1);

        protected static readonly DetailedResult _fakeSuccessDetailedResult = new DetailedResult(true, null, _fakeSuccessCmdResult);
        protected static readonly DetailedResult _fakeFailureDetailedResult = new DetailedResult(false, "junk error", _fakeFailureCmdResult);

        protected static readonly string _fakeProjectPath = "this\\is\\a\\fake\\path\\to\\a\\project\\directory";
        protected static readonly string _fakeManifestPath = "this\\is\\a\\fake\\path\\to\\a\\manifest";
        protected static readonly Action<string> _fakeOutCallback = content => { };
        protected static readonly Action<string> _fakeErrCallback = content => { };

        internal string[] _sampleManifestLines = File.ReadAllLines("TestFakes//fake-manifest.yml");
        internal string[] _sampleInvalidManifestLines = File.ReadAllLines("TestFakes//fake-invalid-manifest.yml");
        internal string[] _multiBuildpackManifestLines = File.ReadAllLines("TestFakes//fake-multi-buildpack-manifest.yml");

        protected static readonly List<CloudFoundryOrganization> _fakeOrgs = new List<CloudFoundryOrganization>
        {
            new CloudFoundryOrganization("fakeOrg1", "fake-org-guid-1", _fakeCfInstance),
            new CloudFoundryOrganization("fakeOrg2", "fake-org-guid-2", _fakeCfInstance),
            new CloudFoundryOrganization("fakeOrg3", "fake-org-guid-3", _fakeCfInstance),
            new CloudFoundryOrganization("fakeOrg4", "fake-org-guid-4", _fakeCfInstance),
            new CloudFoundryOrganization("fakeOrg5", "fake-org-guid-5", _fakeCfInstance),
        };

        protected static readonly List<CloudFoundrySpace> _fakeSpaces = new List<CloudFoundrySpace>
        {
            new CloudFoundrySpace("fakeSpace1", "fake-space-guid-1", _fakeCfOrg),
            new CloudFoundrySpace("fakeSpace2", "fake-space-guid-2", _fakeCfOrg),
            new CloudFoundrySpace("fakeSpace3", "fake-space-guid-3", _fakeCfOrg),
            new CloudFoundrySpace("fakeSpace4", "fake-space-guid-4", _fakeCfOrg),
            new CloudFoundrySpace("fakeSpace5", "fake-space-guid-5", _fakeCfOrg),
        };

        protected static readonly List<CloudFoundryApp> _fakeApps = new List<CloudFoundryApp>
        {
            new CloudFoundryApp("fakeApp1", "fake-app-guid-1", _fakeCfSpace, "junk state"),
            new CloudFoundryApp("fakeApp2", "fake-app-guid-2", _fakeCfSpace, "junk state"),
            new CloudFoundryApp("fakeApp3", "fake-app-guid-3", _fakeCfSpace, "junk state"),
            new CloudFoundryApp("fakeApp4", "fake-app-guid-4", _fakeCfSpace, "junk state"),
            new CloudFoundryApp("fakeApp5", "fake-app-guid-5", _fakeCfSpace, "junk state"),
        };

        internal class FakeOutputView : IView
        {
            public IViewModel ViewModel { get; set; }

            public bool ShowMethodWasCalled { get; private set; }

            public Action DisplayView { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public FakeOutputView()
            {
                ViewModel = new FakeOutputViewModel();
                ShowMethodWasCalled = false;
            }

            public void Show()
            {
                ShowMethodWasCalled = true;
            }
        }

        internal class FakeOutputViewModel : IOutputViewModel, IViewModel
        {
            public FakeOutputViewModel()
            {
                BeginStreamingAppLogsForAppAsyncWasCalled = false;
            }

            public bool BeginStreamingAppLogsForAppAsyncWasCalled { get; set; }

            public Process ActiveProcess { get; set; }
            public object ActiveView { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public List<string> AppendLineInvocationArgs { get; set; }
            public bool OutputPaused { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public bool OutputIsAppLogs { get => throw new NotImplementedException(); set { } }

            public void AppendLine(string newContent)
            {
                AppendLineInvocationArgs ??= new List<string>();
                AppendLineInvocationArgs.Add(newContent);
            }

            public Task BeginStreamingAppLogsForAppAsync(CloudFoundryApp cfApp, IView outputView)
            {
                BeginStreamingAppLogsForAppAsyncWasCalled = true;
                return Task.CompletedTask;
            }

            public void CancelActiveProcess(object arg = null)
            {
                throw new NotImplementedException();
            }

            public void ClearContent(object arg = null)
            {
                throw new NotImplementedException();
            }

            public void PauseOutput(object arg = null)
            {
                throw new NotImplementedException();
            }

            public void ResumeOutput(object arg = null)
            {
                throw new NotImplementedException();
            }
        }

        internal class FakeTasExplorerViewModel : ITasExplorerViewModel
        {
            public FakeTasExplorerViewModel()
            {
            }

            public bool AuthenticationRequired { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public CfInstanceViewModel TasConnection => throw new NotImplementedException();

            public object ActiveView { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public bool CanDisplayRecentAppLogs(object arg)
            {
                throw new NotImplementedException();
            }

            public bool CanInitiateFullRefresh(object arg)
            {
                throw new NotImplementedException();
            }

            public bool CanLogOutTas(object arg)
            {
                throw new NotImplementedException();
            }

            public bool CanOpenDeletionView(object arg)
            {
                throw new NotImplementedException();
            }

            public bool CanOpenLoginView(object arg)
            {
                throw new NotImplementedException();
            }

            public bool CanReAuthenticate(object arg)
            {
                throw new NotImplementedException();
            }

            public bool CanRefreshOrg(object arg)
            {
                throw new NotImplementedException();
            }

            public bool CanRefreshSpace(object arg)
            {
                throw new NotImplementedException();
            }

            public bool CanStartCfApp(object arg)
            {
                throw new NotImplementedException();
            }

            public bool CanStopCfApp(object arg)
            {
                throw new NotImplementedException();
            }

            public Task DisplayRecentAppLogs(object app)
            {
                throw new NotImplementedException();
            }

            public void LogOutTas(object arg)
            {
                throw new NotImplementedException();
            }

            public void OpenDeletionView(object app)
            {
                throw new NotImplementedException();
            }

            public void OpenLoginView(object arg)
            {
                throw new NotImplementedException();
            }

            public void ReAuthenticate(object cf)
            {
                throw new NotImplementedException();
            }

            public void RefreshAllItems(object arg)
            {
                throw new NotImplementedException();
            }

            public Task RefreshOrg(object arg)
            {
                throw new NotImplementedException();
            }

            public Task RefreshSpace(object arg)
            {
                throw new NotImplementedException();
            }

            public void SetConnection(CloudFoundryInstance cf)
            {
                throw new NotImplementedException();
            }

            public Task StartCfApp(object arg)
            {
                throw new NotImplementedException();
            }

            public Task StopCfApp(object arg)
            {
                throw new NotImplementedException();
            }

            public void StreamAppLogs(object app)
            {
                throw new NotImplementedException();
            }
        }

        internal class FakeCfInstanceViewModel : CfInstanceViewModel
        {
            private int _numUpdates = 0;

            internal FakeCfInstanceViewModel(CloudFoundryInstance cloudFoundryInstance, IServiceProvider services, bool expanded = false)
                : base(cloudFoundryInstance, null, services, expanded)
            {
            }

            internal int NumUpdates { get => _numUpdates; private set => _numUpdates = value; }

            protected internal override async Task UpdateAllChildren()
            {
                await Task.Run(() => _numUpdates += 1); // await task to suppress CS1998
            }
        }

        internal class FakeOrgViewModel : OrgViewModel
        {
            private int _numUpdates = 0;

            internal FakeOrgViewModel(CloudFoundryOrganization org, IServiceProvider services, bool expanded = false)
                : base(org, null, null, services, expanded)
            {
            }

            internal int NumUpdates { get => _numUpdates; private set => _numUpdates = value; }

            protected internal override async Task UpdateAllChildren()
            {
                await Task.Run(() => _numUpdates += 1); // await task to suppress CS1998
            }
        }

        internal class FakeSpaceViewModel : SpaceViewModel
        {
            private int _numUpdates = 0;

            internal FakeSpaceViewModel(CloudFoundrySpace space, IServiceProvider services, bool expanded = false)
                : base(space, null, null, services, expanded)
            {
            }

            internal int NumUpdates { get => _numUpdates; private set => _numUpdates = value; }

            protected internal override async Task UpdateAllChildren()
            {
                await Task.Run(() => _numUpdates += 1); // await task to suppress CS1998
            }
        }

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
            MockDotnetCliService = new Mock<IDotnetCliService>();
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
            services.AddSingleton(MockDotnetCliService.Object);
            services.AddSingleton(MockLoginViewModel.Object);
            services.AddSingleton(MockAppDeletionConfirmationViewModel.Object);

            Services = services.BuildServiceProvider();
        }
    }
}
