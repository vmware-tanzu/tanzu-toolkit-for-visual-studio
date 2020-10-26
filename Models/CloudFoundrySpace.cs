namespace TanzuForVS.Models
{
    public class CloudFoundrySpace
    {
        public string SpaceName { get; set; }
        public string SpaceId { get; set; }
        public CloudFoundryOrganization ParentOrg { get; set; }

        public CloudFoundrySpace(string spaceName, string guid, CloudFoundryOrganization parentOrg)
        {
            SpaceName = spaceName;
            SpaceId = guid;
            ParentOrg = parentOrg;
        }

    }
}
