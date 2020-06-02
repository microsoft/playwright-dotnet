namespace PlaywrightSharp.Input
{
    internal class KeyDescription
    {
        public int KeyCode { get; set; }

        public int KeyCodeWithoutLocation { get; set; }

        public string Key { get; set; }

        public string Text { get; set; }

        public string Code { get; set; }

        public double Location { get; set; }
    }
}
