using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarPx.Models
{
    public class AuthResult
    {
        public string? access_token { get; set; }
        public string? api_version { get; set; }
        public long expiry_time { get; set; }
    }
}
