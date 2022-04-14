using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Tanzu.Toolkit.Services
{
    public class CfAppManifestNamingConvention : INamingConvention
    {
        public string Apply(string value)
        {
            var hyphenatedString = HyphenatedNamingConvention.Instance.Apply(value);

            return hyphenatedString switch
            {
                "disk-quota" => "disk_quota",
                "binding-name" => "binding_name",
                "process-types" => "process_types",
                _ => hyphenatedString,
            };
        }

        public static readonly INamingConvention _instance = new CfAppManifestNamingConvention();
    }
}
