using System.Text.Json;

namespace PlaywrightSharp.Transport
{
    internal class PlaywrightServerMessage
    {
       public string Id { get; set; }

       public string Guid { get; set; }

       public string Method { get; set; }

       public JsonElement? Params { get; set; }

       public string Result { get; set; }

       public string Error { get; set; }
    }
}
