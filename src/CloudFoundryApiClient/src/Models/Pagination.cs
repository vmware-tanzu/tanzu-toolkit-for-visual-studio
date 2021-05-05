namespace Tanzu.Toolkit.CloudFoundryApiClient.Models
{
    public class Pagination
    {
        public int total_results { get; set; }
        public int total_pages { get; set; }
        public Href first { get; set; }
        public Href last { get; set; }
        public Href next { get; set; }
        public Href previous { get; set; }
    }
}
