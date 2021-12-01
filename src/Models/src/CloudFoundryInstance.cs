namespace Tanzu.Toolkit.Models
{
    public class CloudFoundryInstance
    {
        public CloudFoundryInstance(string name, string apiAddress)
        {
            InstanceName = name;
            ApiAddress = apiAddress;
        }

        public string InstanceName { get; set; }
        public string ApiAddress { get; set; }
    }
}
