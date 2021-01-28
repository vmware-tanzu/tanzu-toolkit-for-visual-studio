namespace TanzuForVS.Services
{
    public class DetailedResult
    {
        public DetailedResult(bool succeeded, string explanation = null)
        {
            Succeeded = succeeded;
            Explanation = explanation;
        }

        public bool Succeeded { get; set; }
        public string Explanation { get; set; }
    }
}
