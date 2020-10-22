using System;
using System.Collections.Generic;
using System.Text;

namespace TanzuForVS.Models
{
    public class CloudFoundryApp
    {
        public string AppName { get; set; }

        public CloudFoundryApp(string appName)
        {
            AppName = appName;
        }

    }
}
