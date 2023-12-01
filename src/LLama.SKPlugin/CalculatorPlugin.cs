using Microsoft.SemanticKernel;
using NCalc;
using System.Text.RegularExpressions;
using Expression = NCalc.Expression;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.TemplateEngine.Basic;
using Microsoft.SemanticKernel.TemplateEngine;
using LLama.Common;
using LLama;
using LLamaSharp.SemanticKernel.TextCompletion;
using Microsoft.SemanticKernel.AI.TextCompletion;
using LLamaSharp.SemanticKernel.ChatCompletion;

namespace LLama.SKPlugin
{
    public class CalculatorPlugin
    {
        public string SkillName { get; set; } = "CalculatorSkill";
        public string FunctionName { set; get; } = "Calculator";
        int MaxTokens { set; get; }
        double Temperature { set; get; }
        double TopP { set; get; }
        public bool IsProcessing { get; set; } = false;

        Dictionary<string, ISKFunction> ListFunctions = new Dictionary<string, ISKFunction>();

        IKernel kernel { set; get; }

        public CalculatorPlugin(string modelPath)
        {

            // Load weights into memory
            var parameters = new ModelParams(modelPath)
            {
                ContextSize = 1024,
                Seed = 1337,
                GpuLayerCount = 5
            };
            var model = LLamaWeights.LoadFromFile(parameters);
            var ex = new StatelessExecutor(model, parameters);

            var builder = new KernelBuilder();
            builder.WithAIService<ITextCompletion>("local-llama", new LLamaSharpTextCompletion(ex), true);

            kernel = builder.Build();
           

            SetupSkill();
        }

        public void SetupSkill(int MaxTokens = 500, double Temperature = 0.0, double TopP = 1.0)
        {
            this.MaxTokens = MaxTokens;
            this.Temperature = Temperature;
            this.TopP = TopP;

            string skPrompt = @"Translate a math problem into a expression that can be executed using .net NCalc library. Use the output of running this code to answer the question.
Available functions: Abs, Acos, Asin, Atan, Ceiling, Cos, Exp, Floor, IEEERemainder, Log, Log10, Max, Min, Pow, Round, Sign, Sin, Sqrt, Tan, and Truncate. in and if are also supported.

Question: $((Question with math problem.))
expression:``` $((single line mathematical expression that solves the problem))```

[Examples]
Question: What is 37593 * 67?
expression:```37593 * 67```

Question: what is 3 to the 2nd power?
expression:```Pow(3, 2)```

Question: what is sine of 0 radians?
expression:```Sin(0)```

Question: what is sine of 45 degrees?
expression:```Sin(45 * Pi /180 )```

Question: how many radians is 45 degrees?
expression:``` 45 * Pi / 180 ```

Question: what is the square root of 81?
expression:```Sqrt(81)```

Question: what is the angle whose sine is the number 1?
expression:```Asin(1)```

[End of Examples]

Question: {{ $input }}
";
            ChatRequestSettings settings = new() { MaxTokens = MaxTokens, Temperature = Temperature, TopP = TopP };
            var promptConfig = new PromptTemplateConfig()
            {
                Completion = settings
            };

            var promptTemplateFactory = new BasicPromptTemplateFactory();
            var promptTemplate = promptTemplateFactory.Create(
                skPrompt,                        // Prompt template defined in natural language
                promptConfig             // Prompt configuration
            );
            //var CalculatorFunction = kernel.CreateSemanticFunction(skPrompt, requestSettings: settings);
            var CalculatorFunction = kernel.RegisterSemanticFunction(SkillName, FunctionName, promptConfig, promptTemplate);
            ListFunctions.Add(FunctionName, CalculatorFunction);
            Console.WriteLine("Ready..");
        }

       

        public async Task<string> Calculate(string input)
        {
            
            string Result = string.Empty;
            if (IsProcessing) return Result;

            try
            {
                
                IsProcessing = true;
                var answer = await kernel.RunAsync(input, ListFunctions[FunctionName]);

                string pattern = @"```\s*(.*?)\s*```";

                Match match = Regex.Match(answer.GetValue<string>(), pattern, RegexOptions.Singleline);
                if (match.Success)
                {
                    Result = EvaluateMathExpression(match);
                }
                else
                {
                    Result = $"Input value [{input}] could not be understood, received following {answer.GetValue<string>()}";
                }
                //Console.WriteLine(Result);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Result = "error in calculator for input " + input + " " + ex;
                return Result;
            }
            finally
            {
                IsProcessing = false;
            }
            return Result;
        }

        private static string EvaluateMathExpression(Match match)
        {
            var textExpressions = match.Groups[1].Value;
            var expr = new Expression(textExpressions, EvaluateOptions.IgnoreCase);
            expr.EvaluateParameter += delegate (string name, ParameterArgs args)
            {
                args.Result = name.ToLower(System.Globalization.CultureInfo.CurrentCulture) switch
                {
                    "pi" => Math.PI,
                    "e" => Math.E,
                    _ => args.Result
                };
            };

            try
            {
                if (expr.HasErrors())
                {
                    return "Error:" + expr.Error + " could not evaluate " + textExpressions;
                }

                var result = expr.Evaluate();
                return result.ToString();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("could not evaluate " + textExpressions, e);
            }
        }
    }
}
