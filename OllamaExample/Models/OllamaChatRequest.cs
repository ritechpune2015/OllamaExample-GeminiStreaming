namespace OllamaExample.Models
{
    public class OllamaChatRequest
    {
        public string Model { get; set; } = string.Empty;

        public List<LLMMessage> Messages { get; set; } = [];

        public bool Stream { get; set; }

    }
}
