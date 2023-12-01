using LLama.Common;
using LLamaSharp.SemanticKernel.ChatCompletion;
using LLamaSharp.SemanticKernel.TextCompletion;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel;
using System;

namespace LLama.SKPlugin
{
    internal class Program
    {

        static async Task TestSK()
        {
            Console.WriteLine("Example from: https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/KernelSyntaxExamples/Example17_ChatGPT.cs");
            var modelPath = @"C:\Experiment\LLM-Models\llama-2-7b-guanaco-qlora.Q8_0.gguf";
            // Load weights into memory
            var parameters = new ModelParams(modelPath);
            using var model = LLamaWeights.LoadFromFile(parameters);
            var ex = new StatelessExecutor(model, parameters);

            var builder = new KernelBuilder();
            builder.WithAIService<ITextCompletion>("local-llama", new LLamaSharpTextCompletion(ex), true);

            var kernel = builder.Build();

            var prompt = @"{{$input}}

One line TLDR with the fewest words.";

            ChatRequestSettings settings = new() { MaxTokens = 100 };
            var summarize = kernel.CreateSemanticFunction(prompt, requestSettings: settings);

            string text1 = @"
1st Law of Thermodynamics - Energy cannot be created or destroyed.
2nd Law of Thermodynamics - For a spontaneous process, the entropy of the universe increases.
3rd Law of Thermodynamics - A perfect crystal at zero Kelvin has zero entropy.";

            string text2 = @"
1. An object at rest remains at rest, and an object in motion remains in motion at constant speed and in a straight line unless acted on by an unbalanced force.
2. The acceleration of an object depends on the mass of the object and the amount of force applied.
3. Whenever one object exerts a force on another object, the second object exerts an equal and opposite on the first.";

            Console.WriteLine((await kernel.RunAsync(text1, summarize)).GetValue<string>());

            Console.WriteLine((await kernel.RunAsync(text2, summarize)).GetValue<string>());
        }
        static async Task Main(string[] args)
        {
            //await TestSK();
            //Console.ReadLine();
            //https://huggingface.co/TheBloke/llama-2-7B-Guanaco-QLoRA-GGUF
            var modelPath = @"C:\Experiment\LLM-Models\llama-2-7b-guanaco-qlora.Q8_0.gguf";
            var calc = new CalculatorPlugin(modelPath);
            var _userQuestion = string.Empty;
            Console.WriteLine("## LLM Calculator - ask Math Questions ##");
            Console.WriteLine(">> type 'exit' to close");
            while (true)
            {
                try
                {
                    System.Console.Write("Question: ");
                    _userQuestion = Console.ReadLine();
                    if (_userQuestion == "exit") break;
                    if (string.IsNullOrEmpty(_userQuestion))
                    {
                        System.Console.WriteLine("please type math question.");
                        continue;
                    }
                    var answer = await calc.Calculate(_userQuestion);
                    var res = answer;
                    System.Console.WriteLine($"Answer: {res}");
                    
                    _userQuestion = string.Empty;

                }
                catch (Exception ex)
                {
                    Console.WriteLine("error answer question:" + ex);

                }
            }
            System.Console.WriteLine("Close App, Bye ;D");
            
        }
    }
}
