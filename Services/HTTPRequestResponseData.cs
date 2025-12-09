using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoTools.Services
{
    
    public class HTTPRequestResponseData
    {
        public static readonly string CompletedSuccessfully = "Completed Successfully";
        public string result { get; set; } = "Unknown error";
        public object? data { get; set; }
    }
}
