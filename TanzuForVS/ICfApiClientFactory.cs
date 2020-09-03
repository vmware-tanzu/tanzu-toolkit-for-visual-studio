using CloudFoundry.UAA;
using System;

namespace TanzuForVS
{
    public interface ICfApiClientFactory
    {
        IUAA CreateCfApiV2Client(Uri target, Uri httpProxy, bool skipSsl);
    }
}