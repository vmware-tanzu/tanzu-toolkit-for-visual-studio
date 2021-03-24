using System.Text.Json;
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

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    public class DetailedResult<T> : DetailedResult
    {
        public DetailedResult(T content, bool succeeded, string explanation = null, CmdResult cmdDetails = null) : base(succeeded, explanation, cmdDetails)
        {
            Content = content;
        }

        public T Content { get; internal set; }
    }
}
