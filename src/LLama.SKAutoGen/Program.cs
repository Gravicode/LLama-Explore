using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Experimental.Assistants;
using Microsoft.SemanticKernel;

namespace LLama.SKAutoGen
{
    internal class Program
    {
        static void Main(string[] args)
        {
          

            
        }

        private static Task ChatAsync(
        string resourcePath,
        params string[] messages)
        {
            return ChatAsync(resourcePath, null, messages);
        }

        private static async Task ChatAsync(
            string resourcePath,
            IKernelPlugin? plugin,
            params string[] messages)
        {
            var definition = EmbeddedResource.Read(resourcePath);

            var plugins = plugin == null ? new KernelPluginCollection() : new KernelPluginCollection() { plugin };

            var assistant =
                await AssistantBuilder.FromDefinitionAsync(
                    TestConfiguration.OpenAI.ApiKey,
                    OpenAIFunctionEnabledModel,
                    definition,
                    plugins);

            Console.WriteLine($"[{assistant.Id}]");

            var thread = await assistant.NewThreadAsync();
            foreach (var message in messages)
            {
                var messageUser = await thread.AddUserMessageAsync(message).ConfigureAwait(true);
                DisplayMessage(messageUser);

                var assistantMessages = await thread.InvokeAsync(assistant).ConfigureAwait(true);
                DisplayMessages(assistantMessages);
            }
        }

        private static void DisplayMessages(IEnumerable<IChatMessage> messages)
        {
            foreach (var message in messages)
            {
                DisplayMessage(message);
            }
        }

        private static void DisplayMessage(IChatMessage message)
        {
            Console.WriteLine($"[{message.Id}]");
            Console.WriteLine($"# {message.Role}: {message.Content}");
        }
    }
}
