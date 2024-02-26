namespace ExperimentPortalen_API
{
    public class Experiment
    {
        public uint id { get; set; }
        public uint userId { get; set; }
        public string name { get; set; }
        public string title { get; set; } = string.Empty;
        public string desc { get; set; } = string.Empty;
        public string instructions { get; set; } = string.Empty;
        public string materials { get; set; } = string.Empty;
        public uint likeCount { get; set; }
        public List<Category> categories { get; set; } = new List<Category>();
        public List<ImageURL> imageURLs { get; set; } = new List<ImageURL>();
        public List<Comment> comments { get; set; } = new List<Comment>();
    }
}
