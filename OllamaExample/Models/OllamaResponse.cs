namespace OllamaExample.Models
{
    public class OllamaResponse
    {
        public string Model { get; set; } = string.Empty;

        public LLMMessage Message { get; set; } = new();

        public bool Done { get; set; }

    }
}
