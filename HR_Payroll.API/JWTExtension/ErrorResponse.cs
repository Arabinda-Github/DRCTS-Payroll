using Newtonsoft.Json;

namespace HR_Payroll.API.JWTExtension
{
    public class ErrorResponse
    {
        public bool status { get; set; }
        public string? message { get; set; }
        public List<object>? data { get; set; } = new List<object>();
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
