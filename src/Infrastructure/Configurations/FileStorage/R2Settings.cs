using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Configurations.FileStorage
{
    public class R2Settings
    {
        public string AccessKey { get; set; } = null!;
        public string SecretKey { get; set; } = null!;
        public string ServiceUrl { get; set; } = null!;
        public Dictionary<string, string> Buckets { get; set; } = new();
        public Dictionary<string, string> PublicDomains { get; set; } = new();
        public int PresignedUrlExpirationMinutes { get; set; } = 3;
    }
}
