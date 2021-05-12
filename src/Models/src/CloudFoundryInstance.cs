using System;
using System.Collections.ObjectModel;

namespace Tanzu.Toolkit.Models
{
    public class CloudFoundryInstance
    {
        public CloudFoundryInstance(string name, string apiAddress, string accessToken)
        {
            InstanceName = name;
            ApiAddress = apiAddress;
            AccessToken = accessToken;
            InstanceId = Guid.NewGuid().ToString("D"); // 'D' format includes hyphens
        }

        public string InstanceName { get; set; }
        public string InstanceId { get; set; }
        public string ApiAddress { get; set; }
        public string AccessToken { get; set; }
        public ObservableCollection<CloudFoundryOrganization> Orgs { get; set; }
    }
}
