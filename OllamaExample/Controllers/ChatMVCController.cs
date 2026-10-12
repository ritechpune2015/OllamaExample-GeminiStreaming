using Microsoft.AspNetCore.Mvc;
using OllamaExample.Models;
using OllamaExample.Services.Conversation;
using System.Text.Json;

namespace OllamaExample.Controllers
{
    public class ChatMVCController : Controller
    {
        private readonly IConversationService _conversationService;

        public ChatMVCController(IConversationService conversationService)
        {
            _conversationService = conversationService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task Stream(
            [FromBody] ChatRequest request)
        {
            Response.StatusCode = StatusCodes.Status200OK;
            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Append("X-Accel-Buffering", "no");

            var cancellationToken = HttpContext.RequestAborted;

            try
            {
                await foreach (var chunk in
                    _conversationService.StreamMessageAsync(
                        request,
                        cancellationToken))
                {
                    var json = JsonSerializer.Serialize(chunk);

                    await Response.WriteAsync(
                        $"data: {json}\n\n",
                        cancellationToken);

                    await Response.Body.FlushAsync(cancellationToken);
                }

                await Response.WriteAsync(
                    "data: [DONE]\n\n",
                    cancellationToken);

                await Response.Body.FlushAsync(cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // The browser disconnected or cancelled the request.
            }
        }
    }

}

