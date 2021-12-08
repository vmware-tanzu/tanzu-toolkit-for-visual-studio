using System;
using System.Collections.Generic;
using System.Text;

namespace Tanzu.Toolkit.Models
{
    public class CloudFoundryRoute
    {
        public string RouteGuid { get; set; }


        public CloudFoundryRoute(string routeGuid) 
        {
            RouteGuid = routeGuid;
        }
    }
}
