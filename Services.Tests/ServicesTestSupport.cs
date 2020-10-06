using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using TanzuForVS.CloudFoundryApiClient;

namespace TanzuForVS.Services
{
    public abstract class ServicesTestSupport
    {
        protected IServiceProvider services;

        protected Mock<ICfApiClient> mockCfApiClient;

        protected ServicesTestSupport()
        {
            var services = new ServiceCollection();
            mockCfApiClient = new Mock<ICfApiClient>();

            services.AddSingleton<ICfApiClient>(mockCfApiClient.Object);

            this.services = services.BuildServiceProvider();
        }
    }
}
