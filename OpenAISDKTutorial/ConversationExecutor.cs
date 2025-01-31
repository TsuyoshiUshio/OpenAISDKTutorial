﻿using OpenAI.Chat;

namespace OpenAISDKTutorial
{
    public class ConversationExecutor
    {
        private readonly OpenAIClientProvider _clientProvider;
        private readonly SessionManager _sessionManager;
        private readonly ZeroToHeroTool _zeroToHeroTool;

        public ConversationExecutor(OpenAIClientProvider clientProvider, SessionManager sessionManager, ZeroToHeroTool zeroToHeroTool)
        {
            _clientProvider = clientProvider;
            _sessionManager = sessionManager;
            _zeroToHeroTool = zeroToHeroTool;
        }

        public async Task<string> ExecuteConversationAsync(string input)
        {
            var client = _clientProvider.GetChatClient();

            string systemPrompt = ""; // Write your system prompt here
            string userPrompt = input;

            // Configure option in here.
            ChatCompletionOptions options = new ()
            {
                Temperature = 0,
            };

            ChatClient chatClient = _clientProvider.GetChatClient();
            List<ChatMessage> chatMessages = new();

            // Adding history to the chat messages
            chatMessages.AddRange(_sessionManager.GetHistory());

            if (!string.IsNullOrEmpty(systemPrompt))
            {
                chatMessages.Add(new SystemChatMessage(systemPrompt));
            }

            var userChatMessage = new UserChatMessage(userPrompt);
            chatMessages.Add(userChatMessage);
            _sessionManager.AddMessage(userChatMessage);

            options.Tools.Add(_zeroToHeroTool.GetToolDefinition());

            var answer = await chatClient.CompleteChatAsync(chatMessages, options);
            var completion = answer.Value;

            while(completion.FinishReason == ChatFinishReason.ToolCalls)
            {
                chatMessages.Add(new AssistantChatMessage(completion));
                foreach (ChatToolCall toolCall in completion.ToolCalls)
                {
                    string toolAnswer = await _zeroToHeroTool.GetToolCallAsync(toolCall);
                    if (!string.IsNullOrEmpty(toolAnswer))
                    {
                        chatMessages.Add(new ToolChatMessage(toolCall.Id, toolAnswer));
                    }
                }
                answer = await chatClient.CompleteChatAsync(chatMessages, options);
                completion = answer.Value;
            }

            string assistant = "No response from OpenAI";
            if (completion.Content.Count > 0)
            {
                assistant = completion.Content[0].Text;
            }

            _sessionManager.AddMessage(new AssistantChatMessage(assistant));
            return assistant;
        }
    }
}
