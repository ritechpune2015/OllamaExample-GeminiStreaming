using OllamaExample.Entities;
using OllamaExample.Models;
using OllamaExample.Repositories;
using OllamaExample.Services.Memory;
using System.Runtime.CompilerServices;
using System.Text;

namespace OllamaExample.Services.Conversation
{
    public class ConversationService : IConversationService
    {
            private const int RecentMessageCount = 10;

            private const int SummaryTriggerMessageCount = 20;

            private readonly IConversationRepository _repository;

            private readonly IContextBuilder _contextBuilder;

            private readonly ILLMService
                _llmService;

            private readonly IConversationSummaryService
                _summaryService;

            private readonly IMemoryExtractionService
                _memoryExtractionService;

        private readonly IStreamingLLMService _streamingLLMService;
        public ConversationService(
    IConversationRepository repository,
    IContextBuilder contextBuilder,
    ILLMService llmService,
    IConversationSummaryService summaryService,
    IMemoryExtractionService memoryExtractionService,
    IStreamingLLMService streamingLLMService)
        {
            _repository = repository;
            _contextBuilder = contextBuilder;
            _llmService = llmService;
            _summaryService = summaryService;
            _memoryExtractionService = memoryExtractionService;
            _streamingLLMService = streamingLLMService;
        }

  public async IAsyncEnumerable<string> StreamMessageAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                throw new ArgumentException("UserId is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                throw new ArgumentException("Message is required.");
            }

            // 1. Load or create conversation.
            var conversation = request.ConversationId.HasValue
                ? await _repository.GetConversationAsync(
                      request.ConversationId.Value,
                      request.UserId)
                  ?? throw new InvalidOperationException(
                      "Conversation not found.")
                : await _repository.CreateConversationAsync(
                      request.UserId,
                      "New Chat");

            cancellationToken.ThrowIfCancellationRequested();

            // 2. Build the same context used by normal chat.
            var messages = await _contextBuilder.BuildContextAsync(
                conversation,
                request.UserId,
                request.Message);

            cancellationToken.ThrowIfCancellationRequested();

            // 3. Stream Gemini output and accumulate the final answer.
            var fullResponse = new StringBuilder();

            await foreach (
                var chunk in _streamingLLMService.StreamChatAsync(
                    messages,
                    cancellationToken))
            {
                fullResponse.Append(chunk);

                // Send each available chunk to the API caller.
                yield return chunk;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 4. Persist the completed exchange.
            await _repository.AddMessageAsync(
                conversation.ConversationId,
                "user",
                request.Message);

            await _repository.AddMessageAsync(
                conversation.ConversationId,
                "assistant",
                fullResponse.ToString());

            await _repository.SaveChangesAsync();

            // 5. Extract long-term memory after successful completion.
            try
            {
                await _memoryExtractionService.ExtractAndStoreAsync(
                    request.UserId,
                    request.Message,
                    fullResponse.ToString());
            }
            catch (Exception ex)
            {
                // Replace with your application's ILogger logging.
                Console.Error.WriteLine(
                    $"Memory extraction failed: {ex.Message}");
            }

            // 6. Reload the conversation and update summary if needed.
            var updatedConversation =
                await _repository.GetConversationAsync(
                    conversation.ConversationId,
                    request.UserId)
                ?? throw new InvalidOperationException(
                    "Conversation not found after saving.");

            await UpdateSummaryIfRequiredAsync(updatedConversation);
        }


        public async Task<ChatResponse> SendMessageAsync(ChatRequest request)
            {
                ValidateRequest(request);

                // ==================================================
                // 1. GET OR CREATE CONVERSATION
                // ==================================================

                var conversation =
                    request.ConversationId.HasValue

                    ? await _repository
                        .GetConversationAsync(
                            request.ConversationId.Value,
                            request.UserId)
                        ?? throw new InvalidOperationException(
                            "Conversation not found.")

                    : await _repository
                        .CreateConversationAsync(
                            request.UserId,
                            "New Chat");

                // ==================================================
                // 2. BUILD CONTEXT
                // ==================================================

                var messages =
                    await _contextBuilder
                        .BuildContextAsync(
                            conversation,
                            request.UserId,
                            request.Message);

                // ==================================================
                // 3. SEND TO OPENAI
                // ==================================================

                var assistantResponse =
                    await _llmService
                        .ChatAsync(messages);

                // ==================================================
                // 4. SAVE USER MESSAGE
                // ==================================================

                await _repository
                    .AddMessageAsync(
                        conversation.ConversationId,
                        "user",
                        request.Message);

                // ==================================================
                // 5. SAVE ASSISTANT MESSAGE
                // ==================================================

                await _repository
                    .AddMessageAsync(
                        conversation.ConversationId,
                        "assistant",
                        assistantResponse);

                await _repository
                    .SaveChangesAsync();

                // ==================================================
                // 6. AUTOMATIC MEMORY EXTRACTION
                // ==================================================

                try
                {
                    await _memoryExtractionService
                        .ExtractAndStoreAsync(
                            request.UserId,
                            request.Message,
                            assistantResponse);
                }
                catch
                {
                    // In production:
                    // log this exception.
                    //
                    // We intentionally do not fail the
                    // user's chat response because memory
                    // extraction is secondary to the chat.
                }

                // ==================================================
                // 7. RELOAD CONVERSATION
                // ==================================================

                conversation =
                    await _repository
                        .GetConversationAsync(
                            request.ConversationId
                                ?? conversation.ConversationId,
                            request.UserId)
                    ?? throw new InvalidOperationException(
                        "Conversation not found after save.");

                // ==================================================
                // 8. SUMMARY
                // ==================================================

                await UpdateSummaryIfRequiredAsync(conversation);

                // ==================================================
                // 9. RESPONSE
                // ==================================================

                return new ChatResponse
                {
                    ConversationId =
                        conversation.ConversationId,

                    Response =
                        assistantResponse
                };
            }

            private async Task  UpdateSummaryIfRequiredAsync(OllamaExample.Entities.Conversation conversation)
            {
                var orderedMessages =conversation.Messages.OrderBy(x => x.CreatedAt)
                        .ThenBy(x =>x.ConversationMessageId)
                        .ToList();

                // Don't summarize too early.
                if (orderedMessages.Count <
                    SummaryTriggerMessageCount)
                {
                    return;
                }

                // Keep the newest 10 messages
                // as normal conversation context.
                var targetSummaryCount =orderedMessages.Count -
                    RecentMessageCount;

                // Already summarized enough.
                if (targetSummaryCount <=
                    conversation.SummaryMessageCount)
                {
                    return;
                }

                // Find messages that aren't yet represented
                // by the existing summary.
                var messagesToAdd =
                    orderedMessages
                        .Skip(
                            conversation
                                .SummaryMessageCount)
                        .Take(
                            targetSummaryCount -
                            conversation
                                .SummaryMessageCount)
                        .ToList();

                if (messagesToAdd.Count == 0)
                {
                    return;
                }

                // ==================================================
                // GENERATE UPDATED SUMMARY
                // ==================================================

                var updatedSummary =
                    await _summaryService
                        .GenerateSummaryAsync(
                            conversation.Summary,
                            messagesToAdd);

                // ==================================================
                // SAVE UPDATED SUMMARY
                // ==================================================

                await _repository
                    .UpdateSummaryAsync(
                        conversation.ConversationId,
                        updatedSummary,
                        targetSummaryCount);

                await _repository
                    .SaveChangesAsync();
            }

            private static void ValidateRequest(ChatRequest request)
            {
                if (string.IsNullOrWhiteSpace(
                    request.UserId))
                {
                    throw new ArgumentException(
                        "UserId is required.");
                }

                if (string.IsNullOrWhiteSpace(
                    request.Message))
                {
                    throw new ArgumentException(
                        "Message is required.");
                }
            }
        }

    }

