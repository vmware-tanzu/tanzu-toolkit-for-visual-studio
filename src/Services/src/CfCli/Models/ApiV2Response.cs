namespace Tanzu.Toolkit.Services.CfCli.Models
{
    public abstract class ApiV2Response
    {
        public abstract string next_url { get; set; }
        public abstract string prev_url { get; set; }
        public abstract int total_pages { get; set; }
        public abstract int total_results { get; set; }
    }
}
