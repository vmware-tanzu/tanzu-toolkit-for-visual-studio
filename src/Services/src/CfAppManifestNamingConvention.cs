using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Tanzu.Toolkit.Services
{
    public class CfAppManifestNamingConvention : INamingConvention
    {
        public string Apply(string value)
        {
            var hyphenatedString = HyphenatedNamingConvention.Instance.Apply(value);

            switch (hyphenatedString)
            {
                case "disk-quota":
                    return "disk_quota";
                case "binding-name":
                    return "binding_name";
                case "process-types":
                    return "process_types";
                default:
                    return hyphenatedString;
            }
        }

        public static readonly INamingConvention Instance = new CfAppManifestNamingConvention();
    }
}
