namespace ExperimentPortalen_API
{
    public class Comment
    {
        public uint id { get; set; }
        public uint userId { get; set; }
        public string name { get; set; }
        public uint exptId { get; set; }
        public string content { get; set; }
    }
}
