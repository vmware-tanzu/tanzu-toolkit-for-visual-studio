namespace Tanzu.Toolkit.VisualStudio.Models
{
    public class CloudFoundrySpace
    {
        public string SpaceName { get; set; }
        public string SpaceId { get; set; }
        public CloudFoundryOrganization ParentOrg { get; set; }
        public string AppsUrl { get; set; }

        public CloudFoundrySpace(string spaceName, string guid, CloudFoundryOrganization parentOrg, string appsUrl)
        {
            SpaceName = spaceName;
            SpaceId = guid;
            ParentOrg = parentOrg;
            AppsUrl = appsUrl;
        }

    }
}
