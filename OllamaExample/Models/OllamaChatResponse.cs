namespace OllamaExample.Models
{
    public class OllamaChatResponse
    {
        public string Model { get; set; } = string.Empty;
        public LLMMessage Message { get; set; } = new();
        public bool Done { get; set; }

    }
}
