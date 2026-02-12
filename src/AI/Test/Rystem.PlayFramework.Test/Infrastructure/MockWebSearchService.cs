using Rystem.PlayFramework;

namespace Rystem.PlayFramework.Test;

/// <summary>
/// Mock web search service for testing.
/// Simulates real web search behavior with realistic results.
/// </summary>
public sealed class MockWebSearchService : IWebSearchService
{
    private readonly List<WebSearchDocument> _searchIndex;

    public MockWebSearchService()
    {
        // Initialize search index with diverse, realistic web content
        _searchIndex = new List<WebSearchDocument>
        {
            // Tech News
            new WebSearchDocument
            {
                Title = "OpenAI Announces GPT-5 with Enhanced Reasoning Capabilities",
                Url = "https://techcrunch.com/2024/03/15/openai-gpt5-announcement",
                Snippet = "OpenAI today unveiled GPT-5, featuring breakthrough improvements in logical reasoning, " +
                         "mathematical problem-solving, and code generation. The new model demonstrates 40% " +
                         "better performance on MMLU benchmarks compared to GPT-4.",
                Description = "TechCrunch exclusive coverage of OpenAI's latest language model release, " +
                             "including benchmark results, pricing, and availability timeline.",
                RelevanceScore = 0.98,
                PublishedDate = DateTime.UtcNow.AddHours(-6),
                Domain = "techcrunch.com",
                Language = "en"
            },
            
            // Documentation
            new WebSearchDocument
            {
                Title = "ASP.NET Core 9.0 Documentation - Getting Started",
                Url = "https://learn.microsoft.com/aspnet/core/getting-started",
                Snippet = "Learn how to build web apps and services with ASP.NET Core 9.0. This tutorial covers " +
                         "installation, project setup, dependency injection, middleware, and deployment.",
                Description = "Official Microsoft documentation for ASP.NET Core 9.0, including tutorials, " +
                             "API reference, and best practices.",
                RelevanceScore = 0.95,
                PublishedDate = DateTime.UtcNow.AddDays(-30),
                Domain = "learn.microsoft.com",
                Language = "en"
            },
            
            // Stack Overflow
            new WebSearchDocument
            {
                Title = "How to implement async/await in C# 12? - Stack Overflow",
                Url = "https://stackoverflow.com/questions/78456789/async-await-csharp-12",
                Snippet = "Question: I'm trying to understand the best practices for async/await in C# 12. " +
                         "Answer (1,234 upvotes): Use Task.ConfigureAwait(false) in library code to avoid deadlocks. " +
                         "In ASP.NET Core, you don't need ConfigureAwait because there's no SynchronizationContext.",
                Description = "Stack Overflow discussion about async/await patterns in modern C# with 1,234 upvotes.",
                RelevanceScore = 0.92,
                PublishedDate = DateTime.UtcNow.AddDays(-15),
                Domain = "stackoverflow.com",
                Language = "en"
            },
            
            // GitHub Repository
            new WebSearchDocument
            {
                Title = "KeyserDSoze/Rystem: .NET dependency injection framework - GitHub",
                Url = "https://github.com/KeyserDSoze/Rystem",
                Snippet = "Rystem is a comprehensive .NET framework for dependency injection, repository pattern, " +
                         "authentication, and AI integration. Features include factory pattern, discriminated unions, " +
                         "and Play Framework for AI agents. 2.5K stars, 234 forks.",
                Description = "Open source .NET framework hosted on GitHub with extensive documentation and examples.",
                RelevanceScore = 0.90,
                PublishedDate = DateTime.UtcNow.AddDays(-90),
                Domain = "github.com",
                Language = "en"
            },
            
            // Blog Post
            new WebSearchDocument
            {
                Title = "Building Production-Ready AI Agents with .NET - Dev.to",
                Url = "https://dev.to/developer/ai-agents-dotnet-production",
                Snippet = "A comprehensive guide to building reliable AI agents in .NET. Covers LLM integration, " +
                         "tool calling, RAG implementation, cost tracking, and observability with OpenTelemetry.",
                Description = "Practical tutorial for developers building AI agents, including code examples and best practices.",
                RelevanceScore = 0.88,
                PublishedDate = DateTime.UtcNow.AddDays(-7),
                Domain = "dev.to",
                Language = "en"
            },
            
            // Academic Paper
            new WebSearchDocument
            {
                Title = "Retrieval-Augmented Generation for Knowledge-Intensive NLP Tasks (arXiv)",
                Url = "https://arxiv.org/abs/2005.11401",
                Snippet = "We explore retrieval-augmented generation (RAG) approaches that combine pre-trained " +
                         "seq2seq models with dense retrieval systems. RAG models generate text conditioned on retrieved " +
                         "passages from a corpus, improving factual accuracy in knowledge-intensive tasks.",
                Description = "Foundational research paper on RAG by Facebook AI, published in NeurIPS 2020.",
                RelevanceScore = 0.85,
                PublishedDate = new DateTime(2020, 5, 22),
                Domain = "arxiv.org",
                Language = "en"
            },
            
            // News Article
            new WebSearchDocument
            {
                Title = "Microsoft Releases .NET 10 with Native AOT and Enhanced Performance",
                Url = "https://www.infoworld.com/article/dotnet-10-release",
                Snippet = "Microsoft's latest .NET 10 release includes native ahead-of-time compilation, " +
                         "reducing startup time by 60% and memory consumption by 40%. The update also brings " +
                         "C# 14 language features and improved container support.",
                Description = "InfoWorld coverage of .NET 10 launch, including performance benchmarks and new features.",
                RelevanceScore = 0.82,
                PublishedDate = DateTime.UtcNow.AddDays(-45),
                Domain = "infoworld.com",
                Language = "en"
            },
            
            // API Documentation
            new WebSearchDocument
            {
                Title = "Bing Web Search API v7 - Azure Cognitive Services | Microsoft Docs",
                Url = "https://learn.microsoft.com/azure/cognitive-services/bing-web-search/overview",
                Snippet = "The Bing Web Search API provides programmatic access to billions of web pages. " +
                         "Search for text, images, videos, and news with advanced filtering, safe search, " +
                         "and market-specific results. Pricing: $3 per 1,000 queries.",
                Description = "Official documentation for Bing Web Search API, including authentication, query parameters, " +
                             "and response schema.",
                RelevanceScore = 0.80,
                PublishedDate = DateTime.UtcNow.AddDays(-180),
                Domain = "learn.microsoft.com",
                Language = "en"
            },
            
            // Tutorial
            new WebSearchDocument
            {
                Title = "Building a RAG System from Scratch in .NET - A Complete Tutorial",
                Url = "https://codewithdan.com/rag-dotnet-tutorial",
                Snippet = "Learn how to build a Retrieval-Augmented Generation system in .NET. This tutorial covers " +
                         "vector embeddings with OpenAI, storing vectors in Pinecone, implementing similarity search, " +
                         "and integrating with LLMs for contextual responses.",
                Description = "Step-by-step tutorial with code examples, GitHub repository, and live demo.",
                RelevanceScore = 0.78,
                PublishedDate = DateTime.UtcNow.AddDays(-21),
                Domain = "codewithdan.com",
                Language = "en"
            },
            
            // Forum Discussion
            new WebSearchDocument
            {
                Title = "Reddit: Best practices for cost tracking in AI applications?",
                Url = "https://reddit.com/r/MachineLearning/comments/xyz/cost_tracking_ai",
                Snippet = "Discussion thread (245 comments) about tracking costs in production AI systems. " +
                         "Top comment: 'Use OpenTelemetry for metrics, set up budget alerts in Azure, " +
                         "and implement rate limiting per user. Our costs dropped 70% after adding caching.'",
                Description = "Active discussion on Reddit's Machine Learning community about AI cost optimization.",
                RelevanceScore = 0.75,
                PublishedDate = DateTime.UtcNow.AddDays(-3),
                Domain = "reddit.com",
                Language = "en"
            },
            
            // Product Page
            new WebSearchDocument
            {
                Title = "Azure AI Search - Powerful search as a service | Microsoft Azure",
                Url = "https://azure.microsoft.com/products/ai-services/ai-search",
                Snippet = "Azure AI Search (formerly Azure Cognitive Search) provides AI-powered search capabilities " +
                         "for applications. Features include vector search, semantic ranking, faceting, and geo-spatial search. " +
                         "Free tier available, pay-as-you-go pricing starting at $0.10/hour.",
                Description = "Azure AI Search product page with pricing, features, and getting started guide.",
                RelevanceScore = 0.72,
                PublishedDate = DateTime.UtcNow.AddDays(-365),
                Domain = "azure.microsoft.com",
                Language = "en"
            },
            
            // Video
            new WebSearchDocument
            {
                Title = "Building AI Agents with C# and .NET (YouTube) - Microsoft Developer",
                Url = "https://youtube.com/watch?v=abc123xyz",
                Snippet = "1.2M views • 3 weeks ago. Learn how to build intelligent AI agents using C# and .NET. " +
                         "This 45-minute tutorial covers tool calling, memory management, RAG integration, and streaming responses. " +
                         "Presented by Scott Hanselman and featuring live coding demos.",
                Description = "Microsoft Developer YouTube video tutorial on building AI agents in .NET.",
                RelevanceScore = 0.70,
                PublishedDate = DateTime.UtcNow.AddDays(-21),
                Domain = "youtube.com",
                Language = "en"
            }
        };
    }

    public Task<WebSearchResult> SearchAsync(
        WebSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        // Simulate keyword-based web search (simplified version of real search engine ranking)
        var query = request.Query?.ToLowerInvariant() ?? string.Empty;
        var keywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var results = _searchIndex
            .Select(doc => new
            {
                Document = doc,
                Score = CalculateRelevanceScore(doc, keywords, query)
            })
            .Where(x => x.Score > 0.1) // Filter low-relevance results
            .OrderByDescending(x => x.Score)
            .Select(x =>
            {
                // Adjust document score based on search
                var adjustedDoc = new WebSearchDocument
                {
                    Title = x.Document.Title,
                    Url = x.Document.Url,
                    Snippet = x.Document.Snippet,
                    Description = x.Document.Description,
                    RelevanceScore = x.Score,
                    PublishedDate = x.Document.PublishedDate,
                    Domain = x.Document.Domain,
                    Language = x.Document.Language
                };
                return adjustedDoc;
            })
            .ToList();

        // Apply settings
        var maxResults = request.Settings?.MaxResults ?? 10;
        var filteredResults = results.Take(maxResults).ToList();

        // Apply minimum score filter if specified
        if (request.Settings?.MinimumScore.HasValue == true)
        {
            filteredResults = filteredResults
                .Where(doc => doc.RelevanceScore >= request.Settings.MinimumScore.Value)
                .ToList();
        }

        // Apply freshness filter
        if (request.Settings?.Freshness != null && request.Settings.Freshness != WebSearchFreshness.Any)
        {
            var cutoffDate = GetFreshnessCutoffDate(request.Settings.Freshness);
            filteredResults = filteredResults
                .Where(doc => doc.PublishedDate.HasValue && doc.PublishedDate.Value >= cutoffDate)
                .ToList();
        }

        var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

        // Note: Cost calculation delegated to PlayFramework (WebSearchTool)
        return Task.FromResult(new WebSearchResult
        {
            Documents = filteredResults,
            DurationMs = duration,
            ExecutedQuery = request.Query,
            TotalCount = results.Count,
            Offset = request.Offset,
            Cost = null // PlayFramework will calculate using WebSearchCostSettings
        });
    }

    private static double CalculateRelevanceScore(WebSearchDocument doc, string[] keywords, string fullQuery)
    {
        double score = doc.RelevanceScore * 0.5; // Start with document's base score

        var searchableText = $"{doc.Title} {doc.Snippet} {doc.Description} {doc.Domain}".ToLowerInvariant();

        // Exact phrase match (highest weight)
        if (searchableText.Contains(fullQuery))
        {
            score += 0.4;
        }

        // Keyword matches (cumulative)
        foreach (var keyword in keywords)
        {
            if (searchableText.Contains(keyword))
            {
                score += 0.1;
            }
        }

        // Domain authority bonus (edu, gov, microsoft, github)
        if (doc.Domain?.Contains("microsoft.com") == true ||
            doc.Domain?.Contains("github.com") == true ||
            doc.Domain?.Contains(".edu") == true ||
            doc.Domain?.Contains(".gov") == true)
        {
            score += 0.1;
        }

        // Recency bonus (newer content ranks higher)
        if (doc.PublishedDate.HasValue)
        {
            var daysSincePublished = (DateTime.UtcNow - doc.PublishedDate.Value).TotalDays;
            if (daysSincePublished <= 7)
                score += 0.15;
            else if (daysSincePublished <= 30)
                score += 0.10;
            else if (daysSincePublished <= 90)
                score += 0.05;
        }

        return Math.Min(score, 1.0); // Cap at 1.0
    }

    private static DateTime GetFreshnessCutoffDate(WebSearchFreshness freshness)
    {
        return freshness switch
        {
            WebSearchFreshness.Day => DateTime.UtcNow.AddDays(-1),
            WebSearchFreshness.Week => DateTime.UtcNow.AddDays(-7),
            WebSearchFreshness.Month => DateTime.UtcNow.AddDays(-30),
            WebSearchFreshness.Year => DateTime.UtcNow.AddDays(-365),
            _ => DateTime.MinValue
        };
    }
}
