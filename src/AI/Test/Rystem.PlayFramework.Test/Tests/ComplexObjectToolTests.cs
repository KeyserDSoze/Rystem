using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework.Test.Tests;

/// <summary>
/// Tests for complex object parameters in tools.
/// </summary>
public class ComplexObjectToolTests : PlayFrameworkTestBase
{
    /// <summary>
    /// Tests that tools can accept multiple complex object parameters.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithComplexObjects_DeserializesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPlayFramework(builder =>
        {
            builder
                .AddScene("UserManagement", "Manage users with complex data", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<IUserService>(serviceBuilder =>
                        {
                            serviceBuilder.WithMethod(
                                x => x.CreateUserAsync(default, default, default, default),
                                "createUser",
                                "Create a new user with address, contact info, and preferences");
                        });
                });
        });

        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<IChatClient>(sp => new MockComplexObjectChatClient());

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Create user Alessandro", metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert
        var functionCompleted = responses.FirstOrDefault(r => r.Status == AiResponseStatus.FunctionCompleted);
        Assert.NotNull(functionCompleted);
        Assert.Contains("createUser", functionCompleted!.Message);

        // Verify the service was called with complex objects
        var userService = serviceProvider.GetRequiredService<IUserService>() as UserService;
        Assert.NotNull(userService!.LastCreatedUser);
        Assert.Equal("Alessandro Rapiti", userService.LastCreatedUser!.Name);
        Assert.Equal("Via Roma 123", userService.LastCreatedUser.Address.Street);
        Assert.Equal("Milano", userService.LastCreatedUser.Address.City);
        Assert.Equal("alessandro@example.com", userService.LastCreatedUser.Contact.Email);
        Assert.True(userService.LastCreatedUser.Preferences.ReceiveNewsletter);
    }

    /// <summary>
    /// Tests tool with nested complex objects.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithNestedObjects_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPlayFramework(builder =>
        {
            builder
                .AddScene("OrderManagement", "Manage orders", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<IOrderService>(serviceBuilder =>
                        {
                            serviceBuilder.WithMethod(
                                x => x.CreateOrderAsync(default, default),
                                "createOrder",
                                "Create a new order with customer and items");
                        });
                });
        });

        services.AddSingleton<IOrderService, OrderService>();
        services.AddSingleton<IChatClient>(sp => new MockComplexOrderChatClient());

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings { ExecutionMode = SceneExecutionMode.Direct };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Create order for Mario", metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert
        var functionCompleted = responses.FirstOrDefault(r => r.Status == AiResponseStatus.FunctionCompleted);
        Assert.NotNull(functionCompleted);

        var orderService = serviceProvider.GetRequiredService<IOrderService>() as OrderService;
        Assert.NotNull(orderService!.LastOrder);
        Assert.Equal("Mario Rossi", orderService.LastOrder!.Customer.Name);
        Assert.Equal(2, orderService.LastOrder.Items.Count);
        Assert.Equal("Laptop", orderService.LastOrder.Items[0].ProductName);
        Assert.Equal(999.99m, orderService.LastOrder.Items[0].Price);
    }

    /// <summary>
    /// Tests tool with collection parameters.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithCollections_DeserializesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPlayFramework(builder =>
        {
            builder
                .AddScene("BatchProcessing", "Process multiple items", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<IBatchService>(serviceBuilder =>
                        {
                            serviceBuilder.WithMethod(
                                x => x.ProcessItemsAsync(default),
                                "processItems",
                                "Process a list of items");
                        });
                });
        });

        services.AddSingleton<IBatchService, BatchService>();
        services.AddSingleton<IChatClient>(sp => new MockCollectionChatClient());

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings { ExecutionMode = SceneExecutionMode.Direct };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Process batch", metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert
        var functionCompleted = responses.FirstOrDefault(r => r.Status == AiResponseStatus.FunctionCompleted);
        Assert.NotNull(functionCompleted);

        var batchService = serviceProvider.GetRequiredService<IBatchService>() as BatchService;
        Assert.NotNull(batchService!.ProcessedItems);
        Assert.Equal(3, batchService.ProcessedItems!.Count);
        Assert.Equal("Item1", batchService.ProcessedItems[0].Name);
    }
}

#region Domain Models

/// <summary>
/// Address value object.
/// </summary>
public record Address(string Street, string City, string Country);

/// <summary>
/// Contact information.
/// </summary>
public record ContactInfo(string Email, string Phone);

/// <summary>
/// User preferences.
/// </summary>
public record Preferences(string Language, bool ReceiveNewsletter);

/// <summary>
/// User entity.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Address Address { get; set; } = null!;
    public ContactInfo Contact { get; set; } = null!;
    public Preferences Preferences { get; set; } = null!;
}

/// <summary>
/// Customer information.
/// </summary>
public record Customer(string Name, string Email, Address ShippingAddress);

/// <summary>
/// Order item.
/// </summary>
public record OrderItem(string ProductName, int Quantity, decimal Price);

/// <summary>
/// Order entity.
/// </summary>
public class Order
{
    public Guid Id { get; set; }
    public Customer Customer { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Batch processing item.
/// </summary>
public record BatchItem(string Name, string Category, Dictionary<string, object> Metadata);

#endregion

#region Services

public interface IUserService
{
    Task<User> CreateUserAsync(
        string name,
        Address address,
        ContactInfo contact,
        Preferences preferences);
}

public class UserService : IUserService
{
    public User? LastCreatedUser { get; private set; }

    public Task<User> CreateUserAsync(
        string name,
        Address address,
        ContactInfo contact,
        Preferences preferences)
    {
        LastCreatedUser = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Address = address,
            Contact = contact,
            Preferences = preferences
        };

        return Task.FromResult(LastCreatedUser);
    }
}

public interface IOrderService
{
    Task<Order> CreateOrderAsync(Customer customer, List<OrderItem> items);
}

public class OrderService : IOrderService
{
    public Order? LastOrder { get; private set; }

    public Task<Order> CreateOrderAsync(Customer customer, List<OrderItem> items)
    {
        LastOrder = new Order
        {
            Id = Guid.NewGuid(),
            Customer = customer,
            Items = items,
            TotalAmount = items.Sum(i => i.Price * i.Quantity)
        };

        return Task.FromResult(LastOrder);
    }
}

public interface IBatchService
{
    Task<int> ProcessItemsAsync(List<BatchItem> items);
}

public class BatchService : IBatchService
{
    public List<BatchItem>? ProcessedItems { get; private set; }

    public Task<int> ProcessItemsAsync(List<BatchItem> items)
    {
        ProcessedItems = items;
        return Task.FromResult(items.Count);
    }
}

#endregion

#region Mock Chat Clients

internal class MockComplexObjectChatClient : IChatClient
{
    private int _callCount = 0;
    public ChatClientMetadata Metadata => new("mock-complex", null, "mock-1.0");

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);

        _callCount++;

        var responseMessage = new ChatMessage(ChatRole.Assistant, "Processing");

        if (options?.Tools?.Count > 0)
        {
            // First call: scene selection - return UserManagement
            if (_callCount == 1)
            {
                var sceneSelectionCall = new FunctionCallContent(
                    callId: Guid.NewGuid().ToString(),
                    name: "UserManagement", // Scene name
                    arguments: new Dictionary<string, object?>());

                responseMessage.Contents.Add(sceneSelectionCall);
            }
            // Second call: actual tool execution - return createUser function call
            else
            {
                var functionCall = new FunctionCallContent(
                    callId: Guid.NewGuid().ToString(),
                    name: "createUser",
                    arguments: new Dictionary<string, object?>
                    {
                        ["name"] = "Alessandro Rapiti",
                        ["address"] = new Dictionary<string, object?>
                        {
                            ["street"] = "Via Roma 123",
                            ["city"] = "Milano",
                            ["country"] = "Italia"
                        },
                        ["contact"] = new Dictionary<string, object?>
                        {
                            ["email"] = "alessandro@example.com",
                            ["phone"] = "+39 123456789"
                        },
                        ["preferences"] = new Dictionary<string, object?>
                        {
                            ["language"] = "it-IT",
                            ["receiveNewsletter"] = true
                        }
                    });

                responseMessage.Contents.Add(functionCall);
            }
        }

        return new ChatResponse([responseMessage]) { ModelId = "mock-model" };
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose() { }
    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}

internal class MockComplexOrderChatClient : IChatClient
{
    private int _callCount = 0;
    public ChatClientMetadata Metadata => new("mock-order", null, "mock-1.0");

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);

        _callCount++;
        var responseMessage = new ChatMessage(ChatRole.Assistant, "Processing");

        if (options?.Tools?.Count > 0)
        {
            // First call: scene selection
            if (_callCount == 1)
            {
                var sceneSelectionCall = new FunctionCallContent(
                    callId: Guid.NewGuid().ToString(),
                    name: "OrderManagement",
                    arguments: new Dictionary<string, object?>());

                responseMessage.Contents.Add(sceneSelectionCall);
            }
            // Second call: actual tool execution
            else
            {
                var functionCall = new FunctionCallContent(
                    callId: Guid.NewGuid().ToString(),
                    name: "createOrder",
                    arguments: new Dictionary<string, object?>
                    {
                        ["customer"] = new Dictionary<string, object?>
                        {
                            ["name"] = "Mario Rossi",
                            ["email"] = "mario@example.com",
                            ["shippingAddress"] = new Dictionary<string, object?>
                            {
                                ["street"] = "Corso Italia 45",
                                ["city"] = "Roma",
                                ["country"] = "Italia"
                            }
                        },
                        ["items"] = new[]
                        {
                            new Dictionary<string, object?>
                            {
                                ["productName"] = "Laptop",
                                ["quantity"] = 1,
                                ["price"] = 999.99
                            },
                            new Dictionary<string, object?>
                            {
                                ["productName"] = "Mouse",
                                ["quantity"] = 2,
                                ["price"] = 29.99
                            }
                        }
                    });

                responseMessage.Contents.Add(functionCall);
            }
        }

        return new ChatResponse([responseMessage]) { ModelId = "mock-model" };
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose() { }
    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}

internal class MockCollectionChatClient : IChatClient
{
    private int _callCount = 0;
    public ChatClientMetadata Metadata => new("mock-collection", null, "mock-1.0");

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);

        _callCount++;
        var responseMessage = new ChatMessage(ChatRole.Assistant, "Processing");

        if (options?.Tools?.Count > 0)
        {
            // First call: scene selection
            if (_callCount == 1)
            {
                var sceneSelectionCall = new FunctionCallContent(
                    callId: Guid.NewGuid().ToString(),
                    name: "BatchProcessing",
                    arguments: new Dictionary<string, object?>());

                responseMessage.Contents.Add(sceneSelectionCall);
            }
            // Second call: actual tool execution
            else
            {
                var functionCall = new FunctionCallContent(
                    callId: Guid.NewGuid().ToString(),
                    name: "processItems",
                    arguments: new Dictionary<string, object?>
                    {
                        ["items"] = new[]
                        {
                            new Dictionary<string, object?>
                            {
                                ["name"] = "Item1",
                                ["category"] = "Electronics",
                                ["metadata"] = new Dictionary<string, object?>
                                {
                                    ["weight"] = 2.5,
                                    ["color"] = "Black"
                                }
                            },
                            new Dictionary<string, object?>
                            {
                                ["name"] = "Item2",
                                ["category"] = "Books",
                                ["metadata"] = new Dictionary<string, object?>
                                {
                                    ["pages"] = 350,
                                    ["author"] = "John Doe"
                                }
                            },
                            new Dictionary<string, object?>
                            {
                                ["name"] = "Item3",
                                ["category"] = "Clothing",
                                ["metadata"] = new Dictionary<string, object?>
                                {
                                    ["size"] = "M",
                                    ["material"] = "Cotton"
                                }
                            }
                        }
                    });

                responseMessage.Contents.Add(functionCall);
            }
        }

        return new ChatResponse([responseMessage]) { ModelId = "mock-model" };
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose() { }
    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}

#endregion
