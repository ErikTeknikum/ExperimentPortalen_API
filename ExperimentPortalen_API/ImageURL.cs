namespace ExperimentPortalen_API
{
    public class ImageURL
    {
        public uint id { get; set; }
        public string url { get; set; } = string.Empty;
        public string image { get; set; } = string.Empty;
        public uint exptId { get; set; }
    }
}
