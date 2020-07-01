using System.Text.Json;

namespace PlaywrightSharp.Transport
{
    internal class PlaywrightServerMessage
    {
       public int? Id { get; set; }

       public string Guid { get; set; }

       public string Method { get; set; }

       public PlaywrightSharpServerParams Params { get; set; }

       public JsonElement? Result { get; set; }

       public PlaywrightServerError Error { get; set; }
    }
}
