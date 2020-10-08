using System;

namespace TanzuForVS.CloudFoundryApiClient.Models.OrgsResponse
{
    public class OrgsResponse
    {
        public Pagination pagination { get; set; }
        public Resource[] resources { get; set; }
    }

    public class Pagination
    {
        public int total_results { get; set; }
        public int total_pages { get; set; }
        public First first { get; set; }
        public Last last { get; set; }
        public object next { get; set; }
        public object previous { get; set; }
    }

    public class First
    {
        public string href { get; set; }
    }

    public class Last
    {
        public string href { get; set; }
    }

    public class Resource
    {
        public string guid { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string name { get; set; }
        public bool suspended { get; set; }
        public Relationships relationships { get; set; }
        public Links links { get; set; }
        public Metadata metadata { get; set; }
    }

    public class Relationships
    {
        public Quota quota { get; set; }
    }

    public class Quota
    {
        public Data data { get; set; }
    }

    public class Data
    {
        public string guid { get; set; }
    }

    public class Links
    {
        public Self self { get; set; }
        public Domains domains { get; set; }
        public Default_Domain default_domain { get; set; }
        public Quota1 quota { get; set; }
    }

    public class Self
    {
        public string href { get; set; }
    }

    public class Domains
    {
        public string href { get; set; }
    }

    public class Default_Domain
    {
        public string href { get; set; }
    }

    public class Quota1
    {
        public string href { get; set; }
    }

    public class Metadata
    {
        public Labels labels { get; set; }
        public Annotations annotations { get; set; }
    }

    public class Labels
    {
    }

    public class Annotations
    {
    }

}
