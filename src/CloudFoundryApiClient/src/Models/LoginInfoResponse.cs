using System.Collections.Generic;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models
{
    /**
     * Example (from https://docs.cloudfoundry.org/api/uaa/version/75.10.0/index.html#server-information-2)
       {
          "app" : {
            "version" : "75.10.0"
          },
          "links" : {
            "uaa" : "http://localhost:8080/uaa",
            "passwd" : "/forgot_password",
            "login" : "http://localhost:8080/uaa",
            "register" : "/create_account"
          },
          "zone_name" : "uaa",
          "entityID" : "cloudfoundry-saml-login",
          "commit_id" : "git-metadata-not-found",
          "idpDefinitions" : { },
          "prompts" : {
            "username" : [ "text", "Email" ],
            "password" : [ "password", "Password" ]
          },
          "timestamp" : "2021-12-01T09:42:58+0000"
        }
     */
    public class LoginInfoResponse
    {
        /**
         * A list of name/value pairs of configured prompts that the UAA will login a user.
         * Format for each prompt is [type, display name] where type can be 'text' or 'password'
         */
        public Dictionary<string, string[]> Prompts { get; set; }
    }
}