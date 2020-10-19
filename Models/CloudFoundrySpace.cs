namespace TanzuForVS.Models
{
    public class CloudFoundrySpace
    {
        public string SpaceName { get; set; }

        public CloudFoundrySpace(string spaceName)
        {
            SpaceName = spaceName;
        }

    }
}
