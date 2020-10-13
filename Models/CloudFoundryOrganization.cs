namespace TanzuForVS.Models
{
    public class CloudFoundryOrganization
    {
        public string OrgName { get; set; }

        public CloudFoundryOrganization(string orgName)
        {
            OrgName = orgName;
        }
    }
}
