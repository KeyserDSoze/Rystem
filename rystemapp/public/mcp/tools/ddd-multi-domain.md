# DDD Multi-Domain - Enterprise Applications

**Purpose**: This tool explains how to apply **true Domain-Driven Design (DDD)** with **multiple bounded contexts** using Rystem Framework. Use this approach for complex, enterprise-level applications where different business domains need complete isolation.

---

## 🎯 When to Use Multi-Domain

Use multi-domain architecture when:
- ✅ The application has **multiple distinct business areas** (bounded contexts)
- ✅ Different domains have **different business rules** and lifecycles
- ✅ Multiple teams work on **different parts** of the system
- ✅ You need **independent deployment** of domains
- ✅ Different domains have **different scalability** requirements
- ✅ Business complexity is **high**

**Examples**:
- E-Commerce Platform (Orders, Inventory, Shipping, Payments, Customers)
- Hospital System (Patients, Appointments, Billing, Pharmacy, Laboratory)
- ERP System (Sales, Purchasing, Manufacturing, HR, Accounting)
- Logistics Platform (Orders, Shipments, Tracking, Warehouses, Carriers)

---

## 📁 Project Structure

```
src/
├── Orders/                          # Orders Domain (Bounded Context)
│   ├── domains/
│   │   └── [ProjectName].Orders.Core
│   ├── business/
│   │   └── [ProjectName].Orders.Business
│   ├── infrastructures/
│   │   └── [ProjectName].Orders.Storage
│   ├── applications/
│   │   └── [ProjectName].Orders.Api
│   └── tests/
│       └── [ProjectName].Orders.Test
│
├── Shipments/                       # Shipments Domain (Bounded Context)
│   ├── domains/
│   │   └── [ProjectName].Shipments.Core
│   ├── business/
│   │   └── [ProjectName].Shipments.Business
│   ├── infrastructures/
│   │   └── [ProjectName].Shipments.Storage
│   ├── applications/
│   │   └── [ProjectName].Shipments.Api
│   └── tests/
│       └── [ProjectName].Shipments.Test
│
├── Customers/                       # Customers Domain (Bounded Context)
│   ├── domains/
│   │   └── [ProjectName].Customers.Core
│   ├── business/
│   │   └── [ProjectName].Customers.Business
│   ├── infrastructures/
│   │   └── [ProjectName].Customers.Storage
│   ├── applications/
│   │   └── [ProjectName].Customers.Api
│   └── tests/
│       └── [ProjectName].Customers.Test
│
└── app/                             # Frontend Aggregator
    └── [projectname].app/
        ├── src/
        │   ├── domains/
        │   │   ├── orders/         # Orders UI module
        │   │   ├── shipments/      # Shipments UI module
        │   │   └── customers/      # Customers UI module
        │   ├── shared/             # Shared components
        │   └── App.tsx
        └── package.json
```

### Key Characteristics
- **Isolated domains**: Each domain is a **root folder** with complete vertical slice
- **Independent Core**: Each domain has its own entities, rules, and interfaces
- **Separate APIs**: Each domain can be deployed as a separate microservice
- **Domain-specific storage**: Each domain can use different database technologies
- **Unified frontend**: Single React app aggregates all domain UIs (or micro-frontends)

---

## 🧱 Bounded Contexts Explained

### What is a Bounded Context?

A **bounded context** is a **boundary within which a domain model is valid**. The same concept can have different meanings in different contexts.

**Example - E-Commerce**:

**Orders Context**:
```csharp
// In Orders domain, Customer is just a reference
public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }  // Just an ID
    public string CustomerName { get; set; }  // Cached for display
    public List<OrderItem> Items { get; set; }
}
```

**Customers Context**:
```csharp
// In Customers domain, Customer is the aggregate root
public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public Address BillingAddress { get; set; }
    public Address ShippingAddress { get; set; }
    public List<PaymentMethod> PaymentMethods { get; set; }
    // Rich business logic about customer management
}
```

**Shipments Context**:
```csharp
// In Shipments domain, Customer is just a shipping destination
public class Shipment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }  // Reference to Orders domain
    public ShippingAddress Destination { get; set; }  // Denormalized
    public string RecipientName { get; set; }
    public string RecipientPhone { get; set; }
}
```

**Key Insight**: `Customer` means different things in each context. Each domain maintains only the data it needs.

---

## 🏗️ Domain Architecture

### Complete Domain Structure

Each domain folder follows the **same layered architecture**:

```
[DomainName]/
├── domains/                         # Domain Layer
│   └── [ProjectName].[DomainName].Core
│       ├── Entities/               # Aggregates, entities
│       ├── ValueObjects/           # Value objects
│       ├── Interfaces/             # Repository interfaces
│       ├── Events/                 # Domain events
│       └── Services/               # Domain services
│
├── business/                        # Application Layer
│   └── [ProjectName].[DomainName].Business
│       ├── Services/               # Application services
│       ├── UseCases/               # Use case implementations
│       ├── Dtos/                   # Data transfer objects
│       ├── Validators/             # Business validation
│       └── Mappers/                # Entity ↔ DTO mapping
│
├── infrastructures/                 # Infrastructure Layer
│   └── [ProjectName].[DomainName].Storage
│       ├── DbContext/              # EF Core context
│       ├── Repositories/           # Repository implementations
│       ├── Configurations/         # Entity configurations
│       └── Migrations/             # Database migrations
│
├── applications/                    # Presentation Layer
│   └── [ProjectName].[DomainName].Api
│       ├── Controllers/            # REST endpoints
│       ├── Middleware/             # API middleware
│       ├── Configuration/          # DI, settings
│       └── Program.cs              # Entry point
│
└── tests/                           # Test Layer
    └── [ProjectName].[DomainName].Test
        ├── Unit/                   # Unit tests
        ├── Integration/            # Integration tests
        └── Fixtures/               # Test data
```

---

## 🔗 Domain Communication Patterns

### 1. Synchronous Communication (REST APIs)

Domains communicate via HTTP APIs:

```csharp
// In Shipments.Business - calling Orders domain
public class ShipmentService
{
    private readonly IOrdersApiClient _ordersClient;  // HTTP client
    
    public async Task CreateShipmentAsync(Guid orderId)
    {
        // Call Orders API to get order details
        var order = await _ordersClient.GetOrderAsync(orderId);
        
        if (order == null)
            throw new NotFoundException($"Order {orderId} not found");
        
        // Create shipment with order data
        var shipment = new Shipment
        {
            OrderId = orderId,
            Destination = new ShippingAddress
            {
                Street = order.ShippingAddress.Street,
                City = order.ShippingAddress.City,
                // Denormalize what we need
            }
        };
        
        await _shipmentRepository.InsertAsync(shipment);
    }
}
```

### 2. Asynchronous Communication (Events)

Use **domain events** for eventual consistency:

```csharp
// Orders domain - publish event
public class OrderService
{
    private readonly IEventBus _eventBus;
    
    public async Task PlaceOrderAsync(CreateOrderDto dto)
    {
        var order = new Order { /* ... */ };
        await _orderRepository.InsertAsync(order);
        
        // Publish domain event
        await _eventBus.PublishAsync(new OrderPlacedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Items = order.Items.Select(i => new OrderItemEvent { /* ... */ }),
            ShippingAddress = order.ShippingAddress
        });
    }
}

// Shipments domain - subscribe to event
public class OrderPlacedEventHandler : IEventHandler<OrderPlacedEvent>
{
    private readonly IShipmentRepository _shipmentRepository;
    
    public async Task HandleAsync(OrderPlacedEvent evt)
    {
        // Automatically create shipment when order is placed
        var shipment = new Shipment
        {
            OrderId = evt.OrderId,
            Status = ShipmentStatus.Pending,
            Destination = MapAddress(evt.ShippingAddress)
        };
        
        await _shipmentRepository.InsertAsync(shipment);
    }
}
```

### 3. Shared Kernel (Minimal)

Some common types can be shared, but keep it minimal:

```
src/
├── Shared/                          # Shared Kernel (use sparingly)
│   └── [ProjectName].Shared
│       ├── Events/                 # Common event contracts
│       ├── ValueObjects/           # Truly shared value objects (Money, Address)
│       └── Constants/              # Shared constants
```

**⚠️ Warning**: Shared kernel creates coupling. Use sparingly!

---

## 🎯 Real-World Example: Cargo Tracking System

### Domain Breakdown

**1. Orders Domain**
- Entities: `Order`, `OrderItem`
- Responsibilities: Creating orders, managing order lifecycle
- Storage: SQL Server
- API: `/api/orders`

**2. Shipments Domain**
- Entities: `Shipment`, `ShipmentLeg`, `Container`
- Responsibilities: Tracking shipments, managing routes
- Storage: SQL Server
- API: `/api/shipments`

**3. Tracking Domain**
- Entities: `TrackingEvent`, `Location`, `Checkpoint`
- Responsibilities: Recording real-time tracking data
- Storage: Azure Cosmos DB (NoSQL for high write throughput)
- API: `/api/tracking`

**4. Customers Domain**
- Entities: `Customer`, `Address`, `Contact`
- Responsibilities: Customer management, authentication
- Storage: SQL Server
- API: `/api/customers`

**5. Carriers Domain**
- Entities: `Carrier`, `ServiceLevel`, `Rate`
- Responsibilities: Carrier integration, rate calculation
- Storage: SQL Server
- API: `/api/carriers`

### Example Implementation

**Orders Domain - Order Entity**:
```csharp
namespace CargoLens.Orders.Core.Entities;

public class Order : IEntity<Guid>
{
    public Guid Id { get; init; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }  // Reference to Customers domain
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ShippedAt { get; private set; }
    
    // Shipping details (denormalized)
    public ShippingAddress Origin { get; set; }
    public ShippingAddress Destination { get; set; }
    
    public List<OrderItem> Items { get; set; } = new();
    
    // Business logic
    public void MarkAsShipped(Guid shipmentId)
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed orders can be shipped");
        
        Status = OrderStatus.Shipped;
        ShippedAt = DateTime.UtcNow;
        
        // Raise domain event
        DomainEvents.Raise(new OrderShippedEvent(Id, shipmentId));
    }
}

public class OrderItem
{
    public Guid Id { get; set; }
    public string Description { get; set; }
    public decimal Weight { get; set; }
    public string WeightUnit { get; set; }
    public Dimensions Dimensions { get; set; }
}
```

**Shipments Domain - Shipment Entity**:
```csharp
namespace CargoLens.Shipments.Core.Entities;

public class Shipment : IEntity<Guid>
{
    public Guid Id { get; init; }
    public string TrackingNumber { get; set; } = string.Empty;
    public Guid OrderId { get; set; }  // Reference to Orders domain
    public Guid CarrierId { get; set; }  // Reference to Carriers domain
    public ShipmentStatus Status { get; private set; }
    
    // Route
    public Location CurrentLocation { get; private set; }
    public Location OriginLocation { get; set; }
    public Location DestinationLocation { get; set; }
    
    public List<ShipmentLeg> Legs { get; set; } = new();
    
    // Business logic
    public void UpdateLocation(Location newLocation, DateTime timestamp)
    {
        CurrentLocation = newLocation;
        Status = CalculateStatus(newLocation);
        
        // Raise domain event
        DomainEvents.Raise(new ShipmentLocationUpdatedEvent(Id, newLocation, timestamp));
    }
    
    private ShipmentStatus CalculateStatus(Location location)
    {
        if (location.Equals(DestinationLocation))
            return ShipmentStatus.Delivered;
        
        if (location.Equals(OriginLocation))
            return ShipmentStatus.AtOrigin;
        
        return ShipmentStatus.InTransit;
    }
}
```

**Tracking Domain - TrackingEvent Entity**:
```csharp
namespace CargoLens.Tracking.Core.Entities;

public class TrackingEvent : IEntity<Guid>
{
    public Guid Id { get; init; }
    public Guid ShipmentId { get; set; }  // Reference to Shipments domain
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; }  // Departed, Arrived, InTransit, Delivered
    public Location Location { get; set; }
    public string Description { get; set; }
    public string Source { get; set; }  // GPS, Carrier API, Manual
    
    // Metadata for NoSQL storage
    public Dictionary<string, string> Metadata { get; set; } = new();
}

// Event handler - listen to Shipments domain events
public class ShipmentLocationUpdatedEventHandler : IEventHandler<ShipmentLocationUpdatedEvent>
{
    private readonly IRepository<TrackingEvent> _trackingRepository;
    
    public async Task HandleAsync(ShipmentLocationUpdatedEvent evt)
    {
        // Create tracking event in Tracking domain
        var trackingEvent = new TrackingEvent
        {
            Id = Guid.NewGuid(),
            ShipmentId = evt.ShipmentId,
            Timestamp = evt.Timestamp,
            EventType = "LocationUpdate",
            Location = evt.NewLocation,
            Source = "System"
        };
        
        await _trackingRepository.InsertAsync(trackingEvent);
    }
}
```

---

## 📦 Database Per Domain

Each domain can use **different database technologies**:

```
Orders Domain       → SQL Server (relational data)
Shipments Domain    → SQL Server (relational data)
Tracking Domain     → Azure Cosmos DB (high write throughput)
Customers Domain    → SQL Server (relational data)
Carriers Domain     → Azure Blob Storage (NoSQL via Rystem)
```

**Configuration Example**:

```csharp
// Orders.Api/Program.cs
services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("OrdersDb")));

services.AddRepository<Order, OrderRepository>(builder =>
{
    builder.WithEntityFramework<OrdersDbContext>();
});

// Tracking.Api/Program.cs
services.AddRepository<TrackingEvent, TrackingEventRepository>(builder =>
{
    builder.WithCosmosDb(options =>
    {
        options.ConnectionString = configuration["CosmosDb:ConnectionString"];
        options.DatabaseName = "TrackingDb";
    });
});
```

---

## 🚀 Deployment Strategies

### Option 1: Microservices (Recommended)

Each domain API is deployed as a **separate service**:

```
Azure Container Apps:
- cargolens-orders-api       → https://orders.cargolens.app
- cargolens-shipments-api    → https://shipments.cargolens.app
- cargolens-tracking-api     → https://tracking.cargolens.app
- cargolens-customers-api    → https://customers.cargolens.app
- cargolens-carriers-api     → https://carriers.cargolens.app

Azure Static Web Apps:
- cargolens-app              → https://app.cargolens.app
```

**Benefits**:
- ✅ Independent scaling
- ✅ Independent deployment
- ✅ Technology diversity
- ✅ Fault isolation

### Option 2: Modular Monolith

All domains in **one deployable unit**, but logically separated:

```
CargoLens.Api (single deployment)
- Hosts all domain APIs via separate controllers
- Shared hosting, separate databases
- Easier to start, can split later
```

**Benefits**:
- ✅ Simpler deployment
- ✅ Easier development
- ✅ Can migrate to microservices later

---

## 🔧 API Gateway Pattern

Use an **API Gateway** to unify domain APIs:

```
Client → API Gateway → Domain APIs

Example:
https://api.cargolens.app/orders     → Orders.Api
https://api.cargolens.app/shipments  → Shipments.Api
https://api.cargolens.app/tracking   → Tracking.Api
```

**Implementation Options**:
- Azure API Management
- Azure Application Gateway
- Kong
- Ocelot (.NET)
- YARP (Yet Another Reverse Proxy)

---

## 📚 Best Practices for Multi-Domain

### 1. Keep Domains Isolated

```csharp
// ✅ GOOD - Each domain has its own entities
// Orders.Core
public class Order { public Guid CustomerId { get; set; } }

// Customers.Core
public class Customer { public Guid Id { get; set; } /* full details */ }

// ❌ BAD - Sharing entities across domains
// Shared.Core
public class Customer { /* ... */ }  // Used by multiple domains
```

### 2. Use Domain Events for Cross-Domain Operations

```csharp
// ✅ GOOD - Async communication via events
public class OrderService
{
    public async Task PlaceOrderAsync(CreateOrderDto dto)
    {
        var order = /* create order */;
        await _orderRepository.InsertAsync(order);
        
        // Publish event - other domains can react
        await _eventBus.PublishAsync(new OrderPlacedEvent { /* ... */ });
    }
}

// ❌ BAD - Direct coupling between domains
public class OrderService
{
    private readonly ShipmentService _shipmentService;  // From another domain!
    
    public async Task PlaceOrderAsync(CreateOrderDto dto)
    {
        var order = /* create order */;
        await _shipmentService.CreateShipmentAsync(order);  // Tight coupling
    }
}
```

### 3. Denormalize Data When Needed

```csharp
// ✅ GOOD - Cache data from other domains
public class Shipment
{
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; }  // Denormalized from Customers domain
    public string CustomerEmail { get; set; }  // For notifications
}

// ❌ BAD - Joining across domain boundaries
public class ShipmentService
{
    public async Task<ShipmentDto> GetShipmentAsync(Guid id)
    {
        var shipment = await _shipmentRepository.GetAsync(id);
        var customer = await _customerApiClient.GetAsync(shipment.CustomerId);  // Every time!
        // ...
    }
}
```

### 4. Define Clear Bounded Context Boundaries

**Ask these questions**:
- Does this entity belong to multiple contexts?
- Do different parts of the business have different rules for this entity?
- Would this entity change for different reasons?

If yes → **Split into separate domains**

---

## 📦 Required Rystem Packages (Per Domain)

Each domain needs:

### Core Project
```xml
<PackageReference Include="Rystem.DependencyInjection" Version="9.1.3" />
```

### Business Project
```xml
<PackageReference Include="Rystem.DependencyInjection" Version="9.1.3" />
```

### Storage Project
```xml
<PackageReference Include="Rystem.DependencyInjection" Version="9.1.3" />
<PackageReference Include="Rystem.RepositoryFramework.Infrastructure.EntityFramework" Version="9.1.3" />
<!-- Or other storage providers: -->
<!-- <PackageReference Include="Rystem.RepositoryFramework.Infrastructure.Azure.Cosmos.Sql" Version="9.1.3" /> -->
<!-- <PackageReference Include="Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob" Version="9.1.3" /> -->
```

### API Project
```xml
<PackageReference Include="Rystem.DependencyInjection.Web" Version="9.1.3" />
<PackageReference Include="Rystem.Api.Server" Version="9.1.3" />
```

### For Event-Driven Architecture
```xml
<PackageReference Include="Rystem.Queue" Version="9.1.3" />
<!-- Or: -->
<!-- <PackageReference Include="MassTransit.Azure.ServiceBus.Core" Version="8.x" /> -->
```

---

## 🔗 Related Tools

- **ddd-single-domain**: For small applications with unified domain
- **repository-setup**: Configure Rystem.RepositoryFramework
- **project-setup**: Scaffold multi-domain project structure
- **background-jobs**: Use Rystem.BackgroundJob for async tasks

---

## ✅ Summary

**Multi-Domain DDD** is for:
- ✅ Complex enterprise applications
- ✅ Multiple bounded contexts
- ✅ Large teams
- ✅ Independent deployment needs

**Key Principles**:
1. Each domain is a **complete vertical slice** in its own folder
2. Domains communicate via **APIs or events** (not direct references)
3. Each domain can have **different databases**
4. Keep **shared kernel minimal**
5. Use **domain events** for eventual consistency

**Result**: Scalable, maintainable, evolvable architecture for complex business domains! 🚀
