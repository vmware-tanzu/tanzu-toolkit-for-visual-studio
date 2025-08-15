namespace Tanzu.Toolkit.Models
{
    public class CloudFoundryInstance
    {
        public CloudFoundryInstance(string name, string apiAddress, bool skipSslCertValidation = false)
        {
            InstanceName = name;
            ApiAddress = apiAddress;
            SkipSslCertValidation = skipSslCertValidation;
        }

        public string InstanceName { get; set; }
        public string ApiAddress { get; set; }
        public bool SkipSslCertValidation { get; set; }
    }
}