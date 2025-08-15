namespace Tanzu.Toolkit.CloudFoundryApiClient.Models
{
    public class Pagination
    {
        public int Total_results { get; set; }
        public int Total_pages { get; set; }
        public HypertextReference First { get; set; }
        public HypertextReference Last { get; set; }
        public HypertextReference Next { get; set; }
        public HypertextReference Previous { get; set; }
    }
}