using LLamaSharp.KernelMemory;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Handlers;
using Microsoft.SemanticKernel;
using LLama.Common;
using LLamaSharp.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Plugins.Memory;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using System.Reflection;

namespace LLama.RAG
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var folderText = "C:\\Experiment\\BRI-Data";//@"/Users/mifmasterz/experiment/LLMDemo/src/LLMDemo.WebCrawler/bin/Debug/net7.0/Crawled";
            bool IsIndexing = false;//set to false if you have index before
            List<RAGItem> Items = new();
            string _userQuestion;
            bool WithFallBack = true; 
            
            Console.WriteLine("Example from: https://github.com/microsoft/kernel-memory/blob/main/examples/101-using-core-nuget/Program.cs");
            //https://huggingface.co/TheBloke/llama-2-7B-Guanaco-QLoRA-GGUF
            var modelPath = @"C:\Experiment\LLM-Models\llama-2-7b-guanaco-qlora.Q8_0.gguf";
            var HelpSvc = new HelpDeskService(modelPath);
            var currentFolder = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var VektorFolder = Path.Combine(currentFolder, "Vektor");
            if (!Directory.Exists(VektorFolder))
            {
                Directory.CreateDirectory(VektorFolder);
            }
            var configVector = new SimpleVectorDbConfig() { StorageType = Microsoft.KernelMemory.FileSystem.DevTools.FileSystemTypes.Disk, Directory = VektorFolder };

            var memory = new KernelMemoryBuilder()
                    .WithLLamaSharpDefaults(new LLamaSharpConfig(modelPath)
                    {
                        DefaultInferenceParams = new Common.InferenceParams
                        {
                            AntiPrompts = new List<string> { "\n\n" }
                        }
                    })
                    .With(new TextPartitioningOptions
                    {
                        MaxTokensPerParagraph = 300,
                        MaxTokensPerLine = 100,
                        OverlappingTokens = 30
                    })
                .WithSimpleVectorDb(configVector)
                .Build();
            /*
            await memory.ImportDocumentAsync(@"./Assets/sample-SK-Readme.pdf", steps: Constants.PipelineWithoutSummary);

            var question = "What's Semantic Kernel?";

            Console.WriteLine($"\n\nQuestion: {question}");

            var answer = await memory.AskAsync(question);

            Console.WriteLine($"\nAnswer: {answer.Result}");

            Console.WriteLine("\n\n  Sources:\n");

            foreach (var x in answer.RelevantSources)
            {
                Console.WriteLine($"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
            }*/
            if (IsIndexing)
            {
                try
                {
                    DirectoryInfo dir = new DirectoryInfo(folderText);
                    foreach (var CurrentFile in dir.GetFiles("*.txt"))
                    {
                        var tagcollection = new TagCollection();
                        tagcollection.Add("user", "user1");
                        tagcollection.Add("filename", CurrentFile.Name);
                        var bytes = File.ReadAllBytes(CurrentFile.FullName);
                        var ms = new MemoryStream(bytes);
                        await memory.ImportDocumentAsync(ms, CurrentFile.Name, documentId: CurrentFile.Name, tagcollection);
                        System.Console.WriteLine($"file {CurrentFile.Name} has been indexed.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("index fail:" + ex);
                }
            }
            System.Console.WriteLine("type 'exit' to get out..");
            while (true)
            {
                try
                {
                    System.Console.Write("Question: ");
                    _userQuestion = Console.ReadLine();
                    if (_userQuestion == "exit") break;
                    if (string.IsNullOrEmpty(_userQuestion))
                    {
                        System.Console.WriteLine("please type a question.");
                        continue;
                    }
                    var answer = await memory.AskAsync(_userQuestion);
                    var res = answer.Result;

                    if (WithFallBack && res.Length < 20 && res.Trim().Contains("INFO NOT FOUND"))
                    {
                        res = await HelpSvc.Ask(_userQuestion);
                    }

                    var newItem = new RAGItem() { Answer = res, CreatedDate = DateTime.Now, Question = _userQuestion };
                    System.Console.Write("Answer: ");
                    System.Console.WriteLine(newItem.Answer);
                    Console.WriteLine("Sources:\n");
                    //only for debug
                    foreach (var x in answer.RelevantSources)
                    {
                        newItem.Sources.Add(new SourceItem() { Link = x.Link, Source = x.SourceName });
                        Console.WriteLine($"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");
                    }
                    //Console.WriteLine($"Question: {_userQuestion}\n\nAnswer: {answer.Result}");
                    Items.Add(newItem);
                    _userQuestion = string.Empty;

                }
                catch (Exception ex)
                {
                    Console.WriteLine("error answer question:" + ex);

                }
                finally
                {
                    //_isReceivingResponse = false;
                }
            }
            System.Console.WriteLine("Close App, Bye ;D");

        }
    }
    public class RAGItem
    {
        public List<SourceItem> Sources { get; set; } = new();
        public string Question { get; set; }
        public string Answer { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    public class SourceItem
    {
        public string Source { get; set; }
        public string Link { get; set; }
    }
    public class HelpDeskService
    {
        public string SkillName { get; set; } = "HelpDeskSkill";
        public string FunctionName { set; get; } = "HelpDesk";
        int MaxTokens { set; get; }
        double Temperature { set; get; }
        double TopP { set; get; }

        Dictionary<string, ISKFunction> ListFunctions = new Dictionary<string, ISKFunction>();
        Microsoft.SemanticKernel.AI.ChatCompletion.ChatHistory chatHistory { set; get; }

        LLamaSharpChatCompletion chatGPT { set; get; }

        public HelpDeskService(string modelPath)
        {
            // Load weights into memory
            var parameters = new ModelParams(modelPath);
            var model = LLamaWeights.LoadFromFile(parameters);
            var context = model.CreateContext(parameters);
            var ex = new InteractiveExecutor(context);
            chatGPT = new LLamaSharpChatCompletion(ex);
            chatHistory = chatGPT.CreateNewChat("Kamu adalah assistant digital BRI bernama Sabrina, kamu dapat menjawab semua pertanyaan terkait produk dan pelayanan Bank BRI, kamu menjawab dengan ramah, sopan, dan professional.");
        }

       

        public async Task<string> Ask(string input)
        {
            try
            {
                chatHistory.AddUserMessage(input);
                string reply = await chatGPT.GenerateMessageAsync(chatHistory);
                chatHistory.AddAssistantMessage(reply);
                return reply;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return "INFO NOT FOUND";
            }
            finally
            {

            }
        }

    }

}
