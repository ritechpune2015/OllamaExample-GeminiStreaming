namespace OllamaExample.Models
{
    public class MemoryExtractionResult
    {
        public bool ShouldRemember { get; set; }
        public List<ExtractedMemory> Memories { get; set; }= [];

    }
}
