namespace Tanzu.Toolkit.CloudFoundryApiClient.Models.BasicInfoResponse
{
    public class BasicInfoResponse
    {
        public Links Links { get; set; }
    }

    public class Links
    {
        public HypertextReference Login { get; set; }
        public HypertextReference Uaa { get; set; }
    }
}