namespace Issue25648
{
    public class Secret
    {
        internal Secret(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public string? Version { get; internal set; }

        public string? Value { get; internal set; }
    }
}
