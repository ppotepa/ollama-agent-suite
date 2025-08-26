using Ollama.Domain.Models;
using Ollama.Domain.Services;

namespace Ollama.Infrastructure.Repositories
{
    /// <summary>
    /// In-memory repository containing all Ollama model descriptors
    /// </summary>
    public sealed class OllamaModelRepository : IModelRepository
    {
        private readonly IReadOnlyList<IModelDescriptor> _models;

        public OllamaModelRepository()
        {
            _models = CreateAllModelDescriptors();
        }

        public IReadOnlyList<IModelDescriptor> GetAllModels() => _models;

        public IModelDescriptor? FindByName(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName))
                return null;

            return _models.FirstOrDefault(m => 
                string.Equals(m.ModelName, modelName, StringComparison.OrdinalIgnoreCase));
        }

        public IReadOnlyList<IModelDescriptor> FindByCapabilities(params ModelCapability[] capabilities)
        {
            if (capabilities == null || capabilities.Length == 0)
                return _models;

            return _models.Where(m => capabilities.All(cap => m.SupportsCapability(cap))).ToList();
        }

        public IReadOnlyList<IModelDescriptor> FindBySizeRange(ModelSize? minSize = null, ModelSize? maxSize = null)
        {
            return _models.Where(m => m.Sizes.Any(size => 
                (minSize == null || size >= minSize) && 
                (maxSize == null || size <= maxSize))).ToList();
        }

        public IReadOnlyList<IModelDescriptor> GetRecommendedModels(ModelUseCase useCase, PerformanceRequirement performanceRequirement = PerformanceRequirement.Balanced)
        {
            var candidates = useCase switch
            {
                ModelUseCase.GeneralChat => _models.Where(m => !m.SupportsCapability(ModelCapability.Embedding)).ToList(),
                ModelUseCase.CodeGeneration => _models.Where(m => m.ModelName.Contains("code") || m.SupportsCapability(ModelCapability.Tools)).ToList(),
                ModelUseCase.ReasoningTasks => _models.Where(m => m.SupportsCapability(ModelCapability.Thinking) || m.ModelName.Contains("deepseek") || m.ModelName.Contains("qwq")).ToList(),
                ModelUseCase.VisionTasks => _models.Where(m => m.SupportsCapability(ModelCapability.Vision)).ToList(),
                ModelUseCase.EmbeddingGeneration => _models.Where(m => m.SupportsCapability(ModelCapability.Embedding)).ToList(),
                ModelUseCase.ToolUsage => _models.Where(m => m.SupportsCapability(ModelCapability.Tools)).ToList(),
                ModelUseCase.ThinkingTasks => _models.Where(m => m.SupportsCapability(ModelCapability.Thinking)).ToList(),
                _ => _models.ToList()
            };

            // Sort by relevance and performance requirement
            return candidates
                .OrderByDescending(m => GetModelScore(m, performanceRequirement))
                .ToList();
        }

        private static int GetModelScore(IModelDescriptor model, PerformanceRequirement requirement)
        {
            var score = 0;
            
            // Base score on capabilities
            score += model.Capabilities.Count * 10;
            
            // Adjust based on performance requirement
            var recommendedSize = model.Sizes.Any() ? model.GetRecommendedSize(requirement) : null;
            if (recommendedSize != null)
            {
                score += requirement switch
                {
                    PerformanceRequirement.Maximum => (int)(recommendedSize.ParameterCount / 1_000_000_000), // Favor larger models
                    PerformanceRequirement.Minimal => 100 - (int)(recommendedSize.ParameterCount / 1_000_000_000), // Favor smaller models
                    _ => 50 // Balanced
                };
            }

            return score;
        }

        private static IReadOnlyList<IModelDescriptor> CreateAllModelDescriptors()
        {
            var models = new List<IModelDescriptor>
            {
                CreateGptOssDescriptor(),
                CreateDeepSeekR1Descriptor(),
                CreateGemma3Descriptor(),
                CreateQwen3Descriptor(),
                CreateLlama31Descriptor(),
                CreateNomicEmbedTextDescriptor(),
                CreateLlama32Descriptor(),
                CreateMistralDescriptor(),
                CreateQwen25Descriptor(),
                CreateLlama3Descriptor(),
                CreateLlavaDescriptor(),
                CreatePhi3Descriptor(),
                CreateGemma2Descriptor(),
                CreateQwen25CoderDescriptor(),
                CreateGemmaDescriptor(),
                CreateQwenDescriptor(),
                CreateMxbaiEmbedLargeDescriptor(),
                CreateQwen2Descriptor(),
                CreatePhi4Descriptor(),
                CreateLlama2Descriptor(),
                CreateMinicpmVDescriptor(),
                CreateCodeLlamaDescriptor(),
                CreateTinyLlamaDescriptor(),
                CreateDolphin3Descriptor(),
                CreateMistralNemoDescriptor(),
                CreateLlama33Descriptor(),
                CreateOlmo2Descriptor(),
                CreateLlama32VisionDescriptor(),
                CreateDeepSeekV3Descriptor(),
                CreateBgeM3Descriptor(),
                CreateQwqDescriptor(),
                CreateMistralSmallDescriptor(),
                CreateSmollm2Descriptor(),
                CreateLlavaLlama3Descriptor(),
                CreateMixtralDescriptor(),
                CreateLlama2UncensoredDescriptor(),
                CreateStarcoder2Descriptor(),
                CreateAllMinilmDescriptor(),
                CreateDeepSeekCoderDescriptor(),
                CreateDeepSeekCoderV2Descriptor(),
                CreateCodeGemmaDescriptor(),
                CreateSnowflakeArcticEmbedDescriptor(),
                CreatePhiDescriptor(),
                CreateDolphinMixtralDescriptor(),
                CreateLlama4Descriptor(),
                CreateOpenThinkerDescriptor(),
                CreateOrcaMiniDescriptor(),
                CreateQwen25VlDescriptor(),
                CreateSmollmDescriptor(),
                CreateWizardlm2Descriptor(),
                CreateCodestralDescriptor()
            };

            return models.AsReadOnly();
        }

        #region Model Descriptor Creation Methods

        private static IModelDescriptor CreateGptOssDescriptor()
        {
            return new OllamaModelDescriptor(
                "gpt-oss",
                "OpenAI's open-weight models designed for powerful reasoning, agentic tasks, and versatile developer use cases.",
                new[] { ModelCapability.Tools, ModelCapability.Thinking },
                new[] { new ModelSize("20b"), new ModelSize("120b") },
                new ModelStatistics("1.4M", "3", "1 week ago"),
                new Uri("https://ollama.com/library/gpt-oss")
            );
        }

        private static IModelDescriptor CreateDeepSeekR1Descriptor()
        {
            return new OllamaModelDescriptor(
                "deepseek-r1",
                "DeepSeek-R1 is a family of open reasoning models with performance approaching that of leading models, such as O3 and Gemini 2.5 Pro.",
                new[] { ModelCapability.Tools, ModelCapability.Thinking },
                new[] { new ModelSize("1.5b"), new ModelSize("7b"), new ModelSize("8b"), new ModelSize("14b"), new ModelSize("32b"), new ModelSize("70b"), new ModelSize("671b") },
                new ModelStatistics("58.9M", "35", "1 month ago"),
                new Uri("https://ollama.com/library/deepseek-r1")
            );
        }

        private static IModelDescriptor CreateGemma3Descriptor()
        {
            return new OllamaModelDescriptor(
                "gemma3",
                "The current, most capable model that runs on a single GPU.",
                new[] { ModelCapability.Vision },
                new[] { new ModelSize("270m"), new ModelSize("1b"), new ModelSize("4b"), new ModelSize("12b"), new ModelSize("27b") },
                new ModelStatistics("13.1M", "26", "1 week ago"),
                new Uri("https://ollama.com/library/gemma3")
            );
        }

        private static IModelDescriptor CreateQwen3Descriptor()
        {
            return new OllamaModelDescriptor(
                "qwen3",
                "Qwen3 is the latest generation of large language models in Qwen series, offering a comprehensive suite of dense and mixture-of-experts (MoE) models.",
                new[] { ModelCapability.Tools, ModelCapability.Thinking },
                new[] { new ModelSize("0.6b"), new ModelSize("1.7b"), new ModelSize("4b"), new ModelSize("8b"), new ModelSize("14b"), new ModelSize("30b"), new ModelSize("32b"), new ModelSize("235b") },
                new ModelStatistics("6.6M", "56", "2 weeks ago"),
                new Uri("https://ollama.com/library/qwen3")
            );
        }

        private static IModelDescriptor CreateLlama31Descriptor()
        {
            return new OllamaModelDescriptor(
                "llama3.1",
                "Llama 3.1 is a new state-of-the-art model from Meta available in 8B, 70B and 405B parameter sizes.",
                new[] { ModelCapability.Tools },
                new[] { new ModelSize("8b"), new ModelSize("70b"), new ModelSize("405b") },
                new ModelStatistics("100.7M", "93", "8 months ago"),
                new Uri("https://ollama.com/library/llama3.1")
            );
        }

        private static IModelDescriptor CreateNomicEmbedTextDescriptor()
        {
            return new OllamaModelDescriptor(
                "nomic-embed-text",
                "A high-performing open embedding model with a large token context window.",
                new[] { ModelCapability.Embedding },
                Array.Empty<ModelSize>(),
                new ModelStatistics("36.6M", "3", "1 year ago"),
                new Uri("https://ollama.com/library/nomic-embed-text")
            );
        }

        private static IModelDescriptor CreateLlama32Descriptor()
        {
            return new OllamaModelDescriptor(
                "llama3.2",
                "Meta's Llama 3.2 goes small with 1B and 3B models.",
                new[] { ModelCapability.Tools },
                new[] { new ModelSize("1b"), new ModelSize("3b") },
                new ModelStatistics("31.4M", "63", "11 months ago"),
                new Uri("https://ollama.com/library/llama3.2")
            );
        }

        private static IModelDescriptor CreateMistralDescriptor()
        {
            return new OllamaModelDescriptor(
                "mistral",
                "The 7B model released by Mistral AI, updated to version 0.3.",
                new[] { ModelCapability.Tools },
                new[] { new ModelSize("7b") },
                new ModelStatistics("18.2M", "84", "1 month ago"),
                new Uri("https://ollama.com/library/mistral")
            );
        }

        private static IModelDescriptor CreateQwen25Descriptor()
        {
            return new OllamaModelDescriptor(
                "qwen2.5",
                "Qwen2.5 models are pretrained on Alibaba's latest large-scale dataset, encompassing up to 18 trillion tokens. The model supports up to 128K tokens and has multilingual support.",
                new[] { ModelCapability.Tools },
                new[] { new ModelSize("0.5b"), new ModelSize("1.5b"), new ModelSize("3b"), new ModelSize("7b"), new ModelSize("14b"), new ModelSize("32b"), new ModelSize("72b") },
                new ModelStatistics("12.8M", "133", "11 months ago"),
                new Uri("https://ollama.com/library/qwen2.5")
            );
        }

        private static IModelDescriptor CreateLlama3Descriptor()
        {
            return new OllamaModelDescriptor(
                "llama3",
                "Meta Llama 3: The most capable openly available LLM to date",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("8b"), new ModelSize("70b") },
                new ModelStatistics("10.6M", "68", "1 year ago"),
                new Uri("https://ollama.com/library/llama3")
            );
        }

        private static IModelDescriptor CreateLlavaDescriptor()
        {
            return new OllamaModelDescriptor(
                "llava",
                "🌋 LLaVA is a novel end-to-end trained large multimodal model that combines a vision encoder and Vicuna for general-purpose visual and language understanding. Updated to version 1.6.",
                new[] { ModelCapability.Vision },
                new[] { new ModelSize("7b"), new ModelSize("13b"), new ModelSize("34b") },
                new ModelStatistics("8.9M", "98", "1 year ago"),
                new Uri("https://ollama.com/library/llava")
            );
        }

        private static IModelDescriptor CreatePhi3Descriptor()
        {
            return new OllamaModelDescriptor(
                "phi3",
                "Phi-3 is a family of lightweight 3B (Mini) and 14B (Medium) state-of-the-art open models by Microsoft.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("3.8b"), new ModelSize("14b") },
                new ModelStatistics("8.1M", "72", "1 year ago"),
                new Uri("https://ollama.com/library/phi3")
            );
        }

        private static IModelDescriptor CreateGemma2Descriptor()
        {
            return new OllamaModelDescriptor(
                "gemma2",
                "Google Gemma 2 is a high-performing and efficient model available in three sizes: 2B, 9B, and 27B.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("2b"), new ModelSize("9b"), new ModelSize("27b") },
                new ModelStatistics("7.8M", "89", "1 year ago"),
                new Uri("https://ollama.com/library/gemma2")
            );
        }

        private static IModelDescriptor CreateQwen25CoderDescriptor()
        {
            return new OllamaModelDescriptor(
                "qwen2.5-coder",
                "Qwen2.5-Coder is the latest series of Code-Specific Qwen large language models (formerly known as CodeQwen).",
                new[] { ModelCapability.Code },
                new[] { new ModelSize("1.5b"), new ModelSize("7b"), new ModelSize("32b") },
                new ModelStatistics("5.5M", "77", "11 months ago"),
                new Uri("https://ollama.com/library/qwen2.5-coder")
            );
        }

        private static IModelDescriptor CreateGemmaDescriptor()
        {
            return new OllamaModelDescriptor(
                "gemma",
                "Gemma is a family of lightweight, state-of-the-art open models built by Google DeepMind. Updated to version 1.1",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("2b"), new ModelSize("7b") },
                new ModelStatistics("5.2M", "79", "1 year ago"),
                new Uri("https://ollama.com/library/gemma")
            );
        }

        private static IModelDescriptor CreateQwenDescriptor()
        {
            return new OllamaModelDescriptor(
                "qwen",
                "Qwen (Qianwen) is a large language model family developed by Alibaba Cloud to support both Chinese and English.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("1.8b"), new ModelSize("4b"), new ModelSize("7b"), new ModelSize("14b"), new ModelSize("72b") },
                new ModelStatistics("4.9M", "115", "1 year ago"),
                new Uri("https://ollama.com/library/qwen")
            );
        }

        private static IModelDescriptor CreateMxbaiEmbedLargeDescriptor()
        {
            return new OllamaModelDescriptor(
                "mxbai-embed-large",
                "State-of-the-art large embedding model from mixedbread ai",
                new[] { ModelCapability.Embedding },
                Array.Empty<ModelSize>(),
                new ModelStatistics("4.7M", "6", "1 year ago"),
                new Uri("https://ollama.com/library/mxbai-embed-large")
            );
        }

        private static IModelDescriptor CreateQwen2Descriptor()
        {
            return new OllamaModelDescriptor(
                "qwen2",
                "Qwen2 is a new series of large language models from Alibaba group",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("0.5b"), new ModelSize("1.5b"), new ModelSize("7b"), new ModelSize("72b") },
                new ModelStatistics("4.5M", "88", "1 year ago"),
                new Uri("https://ollama.com/library/qwen2")
            );
        }

        private static IModelDescriptor CreatePhi4Descriptor()
        {
            return new OllamaModelDescriptor(
                "phi4",
                "Phi-4 is Microsoft's newest small language model, with 14B parameters.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("14b") },
                new ModelStatistics("4.1M", "8", "2 weeks ago"),
                new Uri("https://ollama.com/library/phi4")
            );
        }

        private static IModelDescriptor CreateLlama2Descriptor()
        {
            return new OllamaModelDescriptor(
                "llama2",
                "Llama 2 is a collection of foundation language models ranging from 7B to 70B parameters.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("7b"), new ModelSize("13b"), new ModelSize("70b") },
                new ModelStatistics("3.7M", "109", "1 year ago"),
                new Uri("https://ollama.com/library/llama2")
            );
        }

        private static IModelDescriptor CreateMinicpmVDescriptor()
        {
            return new OllamaModelDescriptor(
                "minicpm-v",
                "MiniCPM-V is a series of end-to-end multimodal LLMs (MLLMs) designed for vision-language understanding.",
                new[] { ModelCapability.Vision },
                new[] { new ModelSize("8b") },
                new ModelStatistics("3.4M", "30", "1 year ago"),
                new Uri("https://ollama.com/library/minicpm-v")
            );
        }

        private static IModelDescriptor CreateCodeLlamaDescriptor()
        {
            return new OllamaModelDescriptor(
                "codellama",
                "Code Llama is a model for generating and discussing code, built on top of Llama 2.",
                new[] { ModelCapability.Code },
                new[] { new ModelSize("7b"), new ModelSize("13b"), new ModelSize("34b") },
                new ModelStatistics("3.2M", "107", "1 year ago"),
                new Uri("https://ollama.com/library/codellama")
            );
        }

        private static IModelDescriptor CreateTinyLlamaDescriptor()
        {
            return new OllamaModelDescriptor(
                "tinyllama",
                "The TinyLlama project is an open endeavor to pretrain a 1.1B Llama model on 3 trillion tokens.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("1.1b") },
                new ModelStatistics("3.2M", "33", "1 year ago"),
                new Uri("https://ollama.com/library/tinyllama")
            );
        }

        private static IModelDescriptor CreateDolphin3Descriptor()
        {
            return new OllamaModelDescriptor(
                "dolphin3",
                "The uncensored Dolphin model from Eric Hartford, based on Llama 3.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("8b"), new ModelSize("70b") },
                new ModelStatistics("2.7M", "50", "1 year ago"),
                new Uri("https://ollama.com/library/dolphin3")
            );
        }

        private static IModelDescriptor CreateMistralNemoDescriptor()
        {
            return new OllamaModelDescriptor(
                "mistral-nemo",
                "A state-of-the-art 12B model with 128k context length, built by Mistral AI in collaboration with NVIDIA.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("12b") },
                new ModelStatistics("2.6M", "48", "1 year ago"),
                new Uri("https://ollama.com/library/mistral-nemo")
            );
        }

        private static IModelDescriptor CreateLlama33Descriptor()
        {
            return new OllamaModelDescriptor(
                "llama3.3",
                "Meta's Llama 3.3 is a large language model with 70B parameters.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("70b") },
                new ModelStatistics("2.4M", "16", "5 weeks ago"),
                new Uri("https://ollama.com/library/llama3.3")
            );
        }

        private static IModelDescriptor CreateOlmo2Descriptor()
        {
            return new OllamaModelDescriptor(
                "olmo2",
                "OLMo 2 is a family of open language models from the Allen Institute for AI.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("7b"), new ModelSize("13b") },
                new ModelStatistics("2.3M", "32", "8 weeks ago"),
                new Uri("https://ollama.com/library/olmo2")
            );
        }

        private static IModelDescriptor CreateLlama32VisionDescriptor()
        {
            return new OllamaModelDescriptor(
                "llama3.2-vision",
                "Llama 3.2-Vision is a collection of multimodal large language models (LLMs) that can reason about images.",
                new[] { ModelCapability.Vision },
                new[] { new ModelSize("11b"), new ModelSize("90b") },
                new ModelStatistics("2.2M", "35", "11 months ago"),
                new Uri("https://ollama.com/library/llama3.2-vision")
            );
        }

        private static IModelDescriptor CreateDeepSeekV3Descriptor()
        {
            return new OllamaModelDescriptor(
                "deepseek-v3",
                "DeepSeek-V3 is a powerful Mixture-of-Experts language model with 685B total parameters.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("685b") },
                new ModelStatistics("2.1M", "5", "6 weeks ago"),
                new Uri("https://ollama.com/library/deepseek-v3")
            );
        }

        private static IModelDescriptor CreateBgeM3Descriptor()
        {
            return new OllamaModelDescriptor(
                "bge-m3",
                "BGE-M3 is a versatile embedding model supporting dense retrieval, multi-vector retrieval, and sparse retrieval.",
                new[] { ModelCapability.Embedding },
                Array.Empty<ModelSize>(),
                new ModelStatistics("2.0M", "7", "1 year ago"),
                new Uri("https://ollama.com/library/bge-m3")
            );
        }

        private static IModelDescriptor CreateQwqDescriptor()
        {
            return new OllamaModelDescriptor(
                "qwq",
                "QwQ is an experimental research model developed by the Qwen team, focused on advancing AI reasoning capabilities.",
                new[] { ModelCapability.Thinking },
                new[] { new ModelSize("32b") },
                new ModelStatistics("1.9M", "12", "8 weeks ago"),
                new Uri("https://ollama.com/library/qwq")
            );
        }

        private static IModelDescriptor CreateMistralSmallDescriptor()
        {
            return new OllamaModelDescriptor(
                "mistral-small",
                "Mistral Small is a lightweight model optimized for latency and cost, suitable for simple tasks.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("22b") },
                new ModelStatistics("1.8M", "26", "1 month ago"),
                new Uri("https://ollama.com/library/mistral-small")
            );
        }

        private static IModelDescriptor CreateSmollm2Descriptor()
        {
            return new OllamaModelDescriptor(
                "smollm2",
                "SmolLM2 is a family of compact language models available in three sizes: 135M, 360M, and 1.7B parameters.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("135m"), new ModelSize("360m"), new ModelSize("1.7b") },
                new ModelStatistics("1.7M", "34", "11 weeks ago"),
                new Uri("https://ollama.com/library/smollm2")
            );
        }

        private static IModelDescriptor CreateLlavaLlama3Descriptor()
        {
            return new OllamaModelDescriptor(
                "llava-llama3",
                "LLaVA-Llama3 is a large multimodal model that combines a vision encoder with Llama 3.",
                new[] { ModelCapability.Vision },
                new[] { new ModelSize("8b") },
                new ModelStatistics("1.7M", "31", "1 year ago"),
                new Uri("https://ollama.com/library/llava-llama3")
            );
        }

        private static IModelDescriptor CreateMixtralDescriptor()
        {
            return new OllamaModelDescriptor(
                "mixtral",
                "A high-quality sparse mixture of experts model with open weights by Mistral AI.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("8x7b"), new ModelSize("8x22b") },
                new ModelStatistics("1.6M", "66", "1 year ago"),
                new Uri("https://ollama.com/library/mixtral")
            );
        }

        private static IModelDescriptor CreateLlama2UncensoredDescriptor()
        {
            return new OllamaModelDescriptor(
                "llama2-uncensored",
                "Uncensored Llama 2 model by George Sung and Jarrad Hope.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("7b"), new ModelSize("70b") },
                new ModelStatistics("1.5M", "42", "1 year ago"),
                new Uri("https://ollama.com/library/llama2-uncensored")
            );
        }

        private static IModelDescriptor CreateStarcoder2Descriptor()
        {
            return new OllamaModelDescriptor(
                "starcoder2",
                "StarCoder2 is the next generation of transparently trained open code LLMs.",
                new[] { ModelCapability.Code },
                new[] { new ModelSize("3b"), new ModelSize("7b"), new ModelSize("15b") },
                new ModelStatistics("1.5M", "47", "1 year ago"),
                new Uri("https://ollama.com/library/starcoder2")
            );
        }

        private static IModelDescriptor CreateAllMinilmDescriptor()
        {
            return new OllamaModelDescriptor(
                "all-minilm",
                "Embedding models on very large sentence level datasets.",
                new[] { ModelCapability.Embedding },
                Array.Empty<ModelSize>(),
                new ModelStatistics("1.4M", "9", "1 year ago"),
                new Uri("https://ollama.com/library/all-minilm")
            );
        }

        private static IModelDescriptor CreateDeepSeekCoderDescriptor()
        {
            return new OllamaModelDescriptor(
                "deepseek-coder",
                "DeepSeek Coder is a series of code language models trained on both English and Chinese texts.",
                new[] { ModelCapability.Code },
                new[] { new ModelSize("1.3b"), new ModelSize("6.7b"), new ModelSize("33b") },
                new ModelStatistics("1.4M", "66", "1 year ago"),
                new Uri("https://ollama.com/library/deepseek-coder")
            );
        }

        private static IModelDescriptor CreateDeepSeekCoderV2Descriptor()
        {
            return new OllamaModelDescriptor(
                "deepseek-coder-v2",
                "An open-source Mixture-of-Experts code language model that achieves performance comparable to GPT4-Turbo in code-specific tasks.",
                new[] { ModelCapability.Code },
                new[] { new ModelSize("16b"), new ModelSize("236b") },
                new ModelStatistics("1.3M", "38", "1 year ago"),
                new Uri("https://ollama.com/library/deepseek-coder-v2")
            );
        }

        private static IModelDescriptor CreateCodeGemmaDescriptor()
        {
            return new OllamaModelDescriptor(
                "codegemma",
                "CodeGemma is a collection of powerful, lightweight models that can perform a variety of coding tasks.",
                new[] { ModelCapability.Code },
                new[] { new ModelSize("2b"), new ModelSize("7b") },
                new ModelStatistics("1.3M", "39", "1 year ago"),
                new Uri("https://ollama.com/library/codegemma")
            );
        }

        private static IModelDescriptor CreateSnowflakeArcticEmbedDescriptor()
        {
            return new OllamaModelDescriptor(
                "snowflake-arctic-embed",
                "A suite of text embedding models by Snowflake, optimized for performance.",
                new[] { ModelCapability.Embedding },
                Array.Empty<ModelSize>(),
                new ModelStatistics("1.2M", "12", "1 year ago"),
                new Uri("https://ollama.com/library/snowflake-arctic-embed")
            );
        }

        private static IModelDescriptor CreatePhiDescriptor()
        {
            return new OllamaModelDescriptor(
                "phi",
                "Phi is a family of open AI models developed by Microsoft. Phi-2 is a 2.7B language model that demonstrates outstanding reasoning and language understanding capabilities.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("2.7b") },
                new ModelStatistics("1.2M", "42", "1 year ago"),
                new Uri("https://ollama.com/library/phi")
            );
        }

        private static IModelDescriptor CreateDolphinMixtralDescriptor()
        {
            return new OllamaModelDescriptor(
                "dolphin-mixtral",
                "An uncensored, fine-tuned model based on the Mixtral mixture of experts model using a variety of data sources.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("8x7b") },
                new ModelStatistics("1.1M", "31", "1 year ago"),
                new Uri("https://ollama.com/library/dolphin-mixtral")
            );
        }

        private static IModelDescriptor CreateLlama4Descriptor()
        {
            return new OllamaModelDescriptor(
                "llama4",
                "Meta's Llama 4 is the latest iteration of their large language model series.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("11b"), new ModelSize("90b") },
                new ModelStatistics("1.1M", "9", "2 weeks ago"),
                new Uri("https://ollama.com/library/llama4")
            );
        }

        private static IModelDescriptor CreateOpenThinkerDescriptor()
        {
            return new OllamaModelDescriptor(
                "openthinker",
                "OpenThinker is a reasoning model trained using the MCTS-DPO framework.",
                new[] { ModelCapability.Thinking },
                new[] { new ModelSize("7b") },
                new ModelStatistics("1.0M", "14", "4 weeks ago"),
                new Uri("https://ollama.com/library/openthinker")
            );
        }

        private static IModelDescriptor CreateOrcaMiniDescriptor()
        {
            return new OllamaModelDescriptor(
                "orca-mini",
                "A general-purpose model ranging from 3 billion to 70 billion parameters.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("3b"), new ModelSize("7b"), new ModelSize("13b"), new ModelSize("70b") },
                new ModelStatistics("1.0M", "73", "1 year ago"),
                new Uri("https://ollama.com/library/orca-mini")
            );
        }

        private static IModelDescriptor CreateQwen25VlDescriptor()
        {
            return new OllamaModelDescriptor(
                "qwen2.5vl",
                "Qwen2.5VL is a vision-language model that can understand images and videos and chat about them.",
                new[] { ModelCapability.Vision },
                new[] { new ModelSize("3b"), new ModelSize("7b"), new ModelSize("72b") },
                new ModelStatistics("995k", "28", "11 months ago"),
                new Uri("https://ollama.com/library/qwen2.5vl")
            );
        }

        private static IModelDescriptor CreateSmollmDescriptor()
        {
            return new OllamaModelDescriptor(
                "smollm",
                "SmolLM is a family of small language models available in three sizes: 135M, 360M, and 1.7B parameters.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("135m"), new ModelSize("360m"), new ModelSize("1.7b") },
                new ModelStatistics("976k", "45", "1 year ago"),
                new Uri("https://ollama.com/library/smollm")
            );
        }

        private static IModelDescriptor CreateWizardlm2Descriptor()
        {
            return new OllamaModelDescriptor(
                "wizardlm2",
                "State of the art large language model from Microsoft AI with improved performance on complex chat, multilingual, reasoning and agent use cases.",
                Array.Empty<ModelCapability>(),
                new[] { new ModelSize("7b") },
                new ModelStatistics("905k", "21", "1 year ago"),
                new Uri("https://ollama.com/library/wizardlm2")
            );
        }

        private static IModelDescriptor CreateCodestralDescriptor()
        {
            return new OllamaModelDescriptor(
                "codestral",
                "Codestral is a cutting-edge generative model that has been specifically designed and optimized for code generation tasks.",
                new[] { ModelCapability.Code },
                new[] { new ModelSize("22b") },
                new ModelStatistics("900k", "24", "1 year ago"),
                new Uri("https://ollama.com/library/codestral")
            );
        }

        #endregion
    }
}
