using System.Text.Json;
using Tanzu.Toolkit.Services.CommandProcess;

namespace Tanzu.Toolkit.Services
{
    public class DetailedResult
    {
        public DetailedResult()
        {
        }

        public DetailedResult(bool succeeded, string explanation = null, CommandResult cmdDetails = null)
        {
            Succeeded = succeeded;
            Explanation = explanation;
            CmdDetails = cmdDetails;
            FailureType = FailureType.None;
        }

        public bool Succeeded { get; set; }
        public string Explanation { get; set; }
        public CommandResult CmdDetails { get; }
        public FailureType FailureType { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    public class DetailedResult<T> : DetailedResult
    {
        public DetailedResult()
        {
        }

        public DetailedResult(T content, bool succeeded, string explanation = null, CommandResult cmdDetails = null) : base(succeeded, explanation, cmdDetails)
        {
            Content = content;
        }

        public T Content { get; internal set; }
    }

    public enum FailureType
    {
        None = 0,
        InvalidRefreshToken = 1,
    }
}
