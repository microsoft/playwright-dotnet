using System.Text.Json.Serialization;

namespace Microsoft.Playwright.Transport
{
    internal class MessageRequest
    {
        public int Id { get; set; }

        public string Guid { get; set; }

        public string Method { get; set; }

        public object Params { get; set; }

        public object Metadata { get; set; }

        [JsonIgnore]
        public bool TreatErrorPropertyAsError { get; set; }
    }
}
