namespace TanzuForVS.Models
{
    public class CloudFoundrySpace
    {
        public string SpaceName { get; set; }
        public string SpaceId { get; set; }

        public CloudFoundrySpace(string spaceName, string guid)
        {
            SpaceName = spaceName;
            SpaceId = guid;
        }

    }
}
