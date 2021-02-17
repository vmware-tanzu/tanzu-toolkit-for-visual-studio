using Tanzu.Toolkit.VisualStudio.Services.CmdProcess;

namespace Tanzu.Toolkit.VisualStudio.Services
{
    public class DetailedResult
    {
        public DetailedResult(bool succeeded, string explanation = null, CmdResult cmdDetails = null)
        {
            Succeeded = succeeded;
            Explanation = explanation;
            CmdDetails = cmdDetails;
        }

        public bool Succeeded { get; set; }
        public string Explanation { get; set; }
        public CmdResult CmdDetails { get; }
    }
}
