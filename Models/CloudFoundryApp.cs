namespace TanzuForVS.Models
{
    public class CloudFoundryApp
    {
        public string AppName { get; set; }
        public string AppId { get; set; }
        public CloudFoundrySpace ParentSpace { get; set; }
        public string State { get; set; }

        public CloudFoundryApp(string appName, string appGuid, CloudFoundrySpace parentSpace)
        {
            AppName = appName;
            AppId = appGuid;
            ParentSpace = parentSpace;
        }

    }
}
