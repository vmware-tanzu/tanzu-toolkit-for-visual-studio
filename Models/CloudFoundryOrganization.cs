namespace Tanzu.Toolkit.VisualStudio.Models
{
    public class CloudFoundryOrganization
    {
        public string OrgName { get; set; }
        public string OrgId { get; set; }
        public CloudFoundryInstance ParentCf { get; set; }
        public string SpacesUrl { get; set; }

        public CloudFoundryOrganization(string orgName, string guid, CloudFoundryInstance parentCf, string spacesUrl)
        {
            OrgName = orgName;
            OrgId = guid;
            ParentCf = parentCf;
            SpacesUrl = spacesUrl;
        }
    }
}
