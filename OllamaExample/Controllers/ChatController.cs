using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OllamaExample.Models;
using OllamaExample.Services.Conversation;
using System.Text.Json;

namespace OllamaExample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IConversationService _conversationService;
        public ChatController(IConversationService conversationService)
        {
            _conversationService = conversationService;

        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return BadRequest("UserId is required.");
            }
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message is required.");
            }
            var result = await _conversationService.SendMessageAsync(request);
            return Ok(result);
        }

        //one token at a time => our query result generate 100 token 

        [HttpPost("stream")]
        public async Task Stream([FromBody] ChatRequest request,
    CancellationToken cancellationToken)
        {
            Response.StatusCode = StatusCodes.Status200OK;
            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Append("X-Accel-Buffering", "no");

            try
            {
                await foreach (
                    var chunk in _conversationService.StreamMessageAsync(
                        request,
                        cancellationToken))
                {
                    // Encode each chunk as a valid SSE data event.
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
                // Client disconnected or cancelled generation.
            }
            catch (NotSupportedException)
            {
                // The response may already have started.
                if (!Response.HasStarted)
                {
                    Response.StatusCode =
                        StatusCodes.Status501NotImplemented;

                    Response.ContentType = "application/json";

                    await Response.WriteAsJsonAsync(
                        new
                        {
                            error = "Streaming is not supported by the selected provider."
                        });
                }
            }
            catch (Exception ex)
            {
                // Log the exception in production.
                Console.Error.WriteLine(ex);

                if (!Response.HasStarted)
                {
                    Response.StatusCode =
                        StatusCodes.Status500InternalServerError;

                    Response.ContentType = "application/json";

                    await Response.WriteAsJsonAsync(
                        new
                        {
                            error = "An error occurred while generating the response."
                        });
                }
            }
        }

    }
}
