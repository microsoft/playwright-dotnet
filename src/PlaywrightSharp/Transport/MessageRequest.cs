namespace PlaywrightSharp.Transport
{
    internal class MessageRequest
    {
        public int Id { get; set; }

        public string Guid { get; set; }

        public string Method { get; set; }

        public object Params { get; set; }

        public bool TreatErrorPropertyAsError { get; set; }
    }
}
