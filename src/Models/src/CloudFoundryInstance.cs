using System;

namespace Tanzu.Toolkit.Models
{
    public class CloudFoundryInstance
    {
        public CloudFoundryInstance(string name, string apiAddress)
        {
            InstanceName = name;
            ApiAddress = apiAddress;
            InstanceId = Guid.NewGuid().ToString("D"); // 'D' format includes hyphens
        }

        public string InstanceName { get; set; }
        public string InstanceId { get; set; }
        public string ApiAddress { get; set; }
    }
}
