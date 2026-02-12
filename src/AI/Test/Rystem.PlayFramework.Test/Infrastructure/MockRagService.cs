using Rystem.PlayFramework;

namespace Rystem.PlayFramework.Test.Infrastructure;

/// <summary>
/// Mock RAG service for testing. Returns realistic fake documents.
/// </summary>
public class MockRagService : IRagService
{
    private readonly List<MockDocument> _knowledgeBase;
    private readonly int _tokensPerQuery;

    public MockRagService()
    {
        _tokensPerQuery = 150; // Simulate text-embedding-ada-002
        _knowledgeBase = CreateKnowledgeBase();
    }

    public Task<RagResult> SearchAsync(RagRequest request, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Simulate search by keyword matching (real implementation would use vector similarity)
        var queryLower = request.Query.ToLowerInvariant();
        var matches = _knowledgeBase
            .Select(doc => new
            {
                Document = doc,
                Score = CalculateSimulatedScore(queryLower, doc)
            })
            .Where(x => x.Score > (request.Settings.MinimumScore ?? 0))
            .OrderByDescending(x => x.Score)
            .Take(request.Settings.TopK)
            .ToList();

        var results = matches.Select(m => new RagDocument
        {
            Content = m.Document.Content,
            Title = m.Document.Title,
            Source = m.Document.Source,
            Score = m.Score,
            Metadata = new Dictionary<string, object>
            {
                { "category", m.Document.Category },
                { "last_updated", m.Document.LastUpdated }
            }
        }).ToList();

        sw.Stop();

        return Task.FromResult(new RagResult
        {
            Documents = results,
            DurationMs = sw.Elapsed.TotalMilliseconds,
            ExecutedQuery = request.Query,
            TotalCount = matches.Count,
            TokenUsage = new RagTokenUsage
            {
                EmbeddingTokens = _tokensPerQuery,
                SearchTokens = 0 // Vector search doesn't use tokens
            }
            // Cost will be calculated by PlayFramework using RagCostSettings
        });
    }

    private static double CalculateSimulatedScore(string query, MockDocument doc)
    {
        // Simple keyword matching simulation (real implementation uses cosine similarity)
        var keywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var contentLower = doc.Content.ToLowerInvariant();
        var titleLower = doc.Title.ToLowerInvariant();

        var matchCount = keywords.Count(keyword => 
            contentLower.Contains(keyword) || titleLower.Contains(keyword));

        // Boost title matches
        var titleMatchCount = keywords.Count(keyword => titleLower.Contains(keyword));

        // Simulate realistic scores (0.0 - 1.0)
        var score = (matchCount * 0.15) + (titleMatchCount * 0.25);
        return Math.Min(score, 1.0);
    }

    private static List<MockDocument> CreateKnowledgeBase()
    {
        return new List<MockDocument>
        {
            // Customer Support Documents
            new()
            {
                Title = "Password Reset Guide",
                Content = "To reset your password, click on 'Forgot Password' on the login page. " +
                         "Enter your email address and we'll send you a reset link. " +
                         "The link expires in 24 hours for security reasons. " +
                         "If you don't receive the email, check your spam folder.",
                Source = "support/authentication.md",
                Category = "Authentication",
                LastUpdated = new DateTime(2024, 1, 15)
            },
            new()
            {
                Title = "Account Security Best Practices",
                Content = "Enable two-factor authentication (2FA) for enhanced security. " +
                         "Use a strong password with at least 12 characters, including uppercase, lowercase, numbers, and symbols. " +
                         "Never share your password with anyone. " +
                         "Change your password every 90 days.",
                Source = "support/security.md",
                Category = "Security",
                LastUpdated = new DateTime(2024, 1, 10)
            },
            new()
            {
                Title = "Billing and Subscription",
                Content = "Our service offers three subscription tiers: Basic ($9.99/month), Pro ($29.99/month), and Enterprise (custom pricing). " +
                         "You can upgrade or downgrade your plan at any time. " +
                         "All plans include a 14-day free trial. " +
                         "Billing is processed on the first day of each month.",
                Source = "support/billing.md",
                Category = "Billing",
                LastUpdated = new DateTime(2024, 2, 1)
            },
            new()
            {
                Title = "Refund Policy",
                Content = "We offer a 30-day money-back guarantee for annual subscriptions. " +
                         "Monthly subscriptions can be cancelled at any time, effective at the end of the billing period. " +
                         "Refunds are processed within 5-7 business days. " +
                         "Contact support@example.com to request a refund.",
                Source = "support/refunds.md",
                Category = "Billing",
                LastUpdated = new DateTime(2023, 12, 20)
            },

            // Product Documentation
            new()
            {
                Title = "Getting Started Tutorial",
                Content = "Welcome to our platform! Start by creating an account and completing your profile. " +
                         "Next, explore the dashboard to familiarize yourself with the interface. " +
                         "Our interactive tutorial will guide you through the key features. " +
                         "Check out our video tutorials for visual learners.",
                Source = "docs/getting-started.md",
                Category = "Tutorial",
                LastUpdated = new DateTime(2024, 1, 5)
            },
            new()
            {
                Title = "API Integration Guide",
                Content = "Integrate our API into your application using our REST or GraphQL endpoints. " +
                         "Authentication requires an API key, which you can generate from your dashboard. " +
                         "Rate limits: 1000 requests/hour for Basic, 10000/hour for Pro, unlimited for Enterprise. " +
                         "SDKs available for Python, JavaScript, C#, Java, and Ruby.",
                Source = "docs/api/integration.md",
                Category = "API",
                LastUpdated = new DateTime(2024, 2, 10)
            },
            new()
            {
                Title = "API Authentication",
                Content = "Use Bearer token authentication for all API requests. " +
                         "Include your API key in the Authorization header: 'Authorization: Bearer YOUR_API_KEY'. " +
                         "API keys can be rotated for security. We recommend rotating keys every 90 days. " +
                         "Test your authentication using our /api/v1/auth/test endpoint.",
                Source = "docs/api/authentication.md",
                Category = "API",
                LastUpdated = new DateTime(2024, 1, 25)
            },
            new()
            {
                Title = "Webhook Configuration",
                Content = "Configure webhooks to receive real-time notifications for events. " +
                         "Supported events: user.created, payment.succeeded, subscription.updated. " +
                         "Webhooks are sent via POST request with JSON payload. " +
                         "Verify webhook signatures using HMAC-SHA256 to prevent spoofing.",
                Source = "docs/webhooks.md",
                Category = "API",
                LastUpdated = new DateTime(2024, 1, 30)
            },

            // Technical Documentation
            new()
            {
                Title = "Database Schema Design",
                Content = "Our platform uses PostgreSQL for relational data and Redis for caching. " +
                         "User data is stored in the 'users' table with bcrypt-hashed passwords. " +
                         "Session tokens are stored in Redis with 24-hour TTL. " +
                         "All timestamps are stored in UTC.",
                Source = "docs/architecture/database.md",
                Category = "Architecture",
                LastUpdated = new DateTime(2024, 1, 12)
            },
            new()
            {
                Title = "Performance Optimization Tips",
                Content = "Enable caching for frequently accessed data to reduce database load. " +
                         "Use pagination for large datasets (max 100 items per page). " +
                         "Compress responses using gzip to reduce bandwidth. " +
                         "Implement exponential backoff for retry logic.",
                Source = "docs/performance.md",
                Category = "Performance",
                LastUpdated = new DateTime(2024, 2, 5)
            },

            // Troubleshooting
            new()
            {
                Title = "Common Error Codes",
                Content = "Error 401: Unauthorized - Check your API key or login credentials. " +
                         "Error 403: Forbidden - Insufficient permissions for this resource. " +
                         "Error 429: Too Many Requests - Rate limit exceeded, retry after 60 seconds. " +
                         "Error 500: Internal Server Error - Contact support if issue persists.",
                Source = "docs/troubleshooting/errors.md",
                Category = "Troubleshooting",
                LastUpdated = new DateTime(2024, 1, 20)
            },
            new()
            {
                Title = "Connection Timeout Issues",
                Content = "If experiencing connection timeouts, check your network settings. " +
                         "Ensure firewall allows outbound HTTPS connections on port 443. " +
                         "Try increasing request timeout to 30 seconds for large payloads. " +
                         "Use keep-alive connections to reduce latency.",
                Source = "docs/troubleshooting/network.md",
                Category = "Troubleshooting",
                LastUpdated = new DateTime(2023, 12, 15)
            }
        };
    }

    private class MockDocument
    {
        public required string Title { get; init; }
        public required string Content { get; init; }
        public required string Source { get; init; }
        public required string Category { get; init; }
        public required DateTime LastUpdated { get; init; }
    }
}
