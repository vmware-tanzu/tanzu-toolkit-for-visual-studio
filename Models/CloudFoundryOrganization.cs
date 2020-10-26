namespace TanzuForVS.Models
{
    public class CloudFoundryOrganization
    {
        public string OrgName { get; set; }
        public string OrgId { get; set; }
        public CloudFoundryInstance ParentCf { get; set; }

        public CloudFoundryOrganization(string orgName, string guid, CloudFoundryInstance parentCf)
        {
            OrgName = orgName;
            OrgId = guid;
            ParentCf = parentCf;
        }
    }
}
