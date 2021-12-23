using System.Text.Json;
using Tanzu.Toolkit.Services.CommandProcess;

namespace Tanzu.Toolkit.Services
{
    public class DetailedResult
    {
        public DetailedResult()
        {
        }

        public DetailedResult(bool succeeded, string explanation = null, CommandResult cmdResult = null)
        {
            Succeeded = succeeded;
            Explanation = explanation;
            CmdResult = cmdResult;
            FailureType = FailureType.None;
        }

        public bool Succeeded { get; set; }
        public string Explanation { get; set; }
        public CommandResult CmdResult { get; }
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

        public T Content { get; set; }
    }

    public enum FailureType
    {
        None = 0,
        InvalidRefreshToken = 1,
        MissingSsoPrompt = 2,
        InvalidCertificate = 3,
    }
}
