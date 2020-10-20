namespace TanzuForVS.Models
{
    public class CloudFoundryOrganization
    {
        public string OrgName { get; set; }
        public string OrgId { get; set; }


        public CloudFoundryOrganization(string orgName, string guid)
        {
            OrgName = orgName;
            OrgId = guid;
        }
    }
}
