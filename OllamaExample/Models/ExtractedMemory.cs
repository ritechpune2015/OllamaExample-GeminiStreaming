namespace OllamaExample.Models
{
    public class ExtractedMemory
    {
        public string Content { get; set; }
            = string.Empty;

        public string Type { get; set; }
            = "Fact";

        public int Importance { get; set; }

    }
}
