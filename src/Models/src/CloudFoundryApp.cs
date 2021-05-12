namespace Tanzu.Toolkit.Models
{
    public class CloudFoundryApp
    {
        public string AppName { get; set; }
        public string AppId { get; set; }
        public CloudFoundrySpace ParentSpace { get; set; }
        public string State { get; set; }

        public CloudFoundryApp(string appName, string appGuid, CloudFoundrySpace parentSpace, string state)
        {
            AppName = appName;
            AppId = appGuid;
            ParentSpace = parentSpace;
            State = state;
        }
    }
}
