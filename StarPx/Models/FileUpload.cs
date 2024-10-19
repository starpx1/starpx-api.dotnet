using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarPx.Models
{
    public class FileUploadStartRequest
    {
        public string? path { get; set; }
        public string? compression { get; set; }
        public long? size { get; set; }
    }
    public class FileUploadStartResponse
    {
        public string? upload_url { get; set; }
        public string? file_id { get; set; }
    }
    public class UploadFileResult
    {
        public string? SuccessMessage { get; set; }
        public string? UploadUrl { get; set; }
    }
}
