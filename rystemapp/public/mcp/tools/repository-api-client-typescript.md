# Repository API Client - TypeScript/JavaScript

**Purpose**: This tool explains how to **consume auto-generated Repository APIs** from TypeScript/JavaScript applications (React, React Native, Next.js, Node.js). The client provides type-safe access to your backend repositories with automatic authentication, error handling, and retry logic.

---

## 🎯 What is Rystem Repository Client?

The **rystem.repository.client** npm package provides:
- ✅ **Type-safe repository access** with TypeScript generics
- ✅ **Automatic authentication** with token refresh
- ✅ **Custom error handlers** with retry logic
- ✅ **CQRS support** (Command/Query separation)
- ✅ **LINQ-style queries** from client-side
- ✅ **Batch operations** support
- ✅ **Works everywhere**: React, React Native, Next.js, Node.js

**Key Benefit**: Configure once, use type-safe repositories throughout your app!

---

## 📦 Installation

```bash
npm install rystem.repository.client
# or
yarn add rystem.repository.client
# or
pnpm add rystem.repository.client
```

---

## 🚀 Quick Start

### Step 1: Define Your Types

Create TypeScript interfaces matching your C# entities:

```typescript
// types/order.ts

// Raw interface (matching C# JsonPropertyName attributes)
export interface OrderRaw {
  i: string;      // Id
  n: string;      // OrderNumber
  c: string;      // CustomerId
  s: number;      // Status (enum as number)
  a: number;      // TotalAmount
  d: string;      // CreatedAt (ISO string)
}

// Clean interface (readable property names)
export interface Order {
  id: string;
  orderNumber: string;
  customerId: string;
  status: OrderStatus;
  totalAmount: number;
  createdAt: Date;
}

export enum OrderStatus {
  Pending = 0,
  Confirmed = 1,
  Shipped = 2,
  Delivered = 3,
  Cancelled = 4
}

// Mapping functions
export function mapRawToOrder(raw: OrderRaw): Order {
  return {
    id: raw.i,
    orderNumber: raw.n,
    customerId: raw.c,
    status: raw.s as OrderStatus,
    totalAmount: raw.a,
    createdAt: new Date(raw.d)
  };
}

export function mapOrderToRaw(order: Order): OrderRaw {
  return {
    i: order.id,
    n: order.orderNumber,
    c: order.customerId,
    s: order.status,
    a: order.totalAmount,
    d: order.createdAt.toISOString()
  };
}
```

### Step 2: Configure Repository Services

```typescript
// config/repositoryConfig.ts
import { RepositoryServices } from "rystem.repository.client";
import { OrderRaw } from "../types/order";
import { CustomerRaw } from "../types/customer";
import { tokenService } from "../services/tokenService";
import { authService } from "../services/authService";

export const setupRepositoryServices = () => {
  const baseUrl = process.env.NEXT_PUBLIC_API_URL || "https://api.myapp.com/api/";

  RepositoryServices
    .Create(baseUrl)
    // Order Repository
    .addRepository<OrderRaw, string>(x => {
      x.name = "Order";
      x.path = "Order";
      x.addHeadersEnricher(() => getAuthHeaders());
      x.addErrorHandler(handleAuthError);
    })
    // Customer Repository
    .addRepository<CustomerRaw, string>(x => {
      x.name = "Customer";
      x.path = "Customer";
      x.addHeadersEnricher(() => getAuthHeaders());
      x.addErrorHandler(handleAuthError);
    });
};

// Helper: Add authorization header
const getAuthHeaders = async (): Promise<HeadersInit> => {
  const accessToken = await tokenService.getValidToken();
  
  return accessToken ? {
    "Authorization": `Bearer ${accessToken}`,
    "Content-Type": "application/json"
  } : {
    "Content-Type": "application/json"
  };
};

// Helper: Handle 401 errors with automatic retry
const handleAuthError = async (
  endpoint: any,
  uri: string,
  method: string,
  headers: any,
  body: any,
  err: any
) => {
  if (err.status === 401) {
    console.log('🔐 401 Unauthorized - attempting token refresh...');
    try {
      const refreshToken = await tokenService.getRefreshToken();
      if (refreshToken) {
        const newTokenData = await authService.refreshToken(refreshToken);
        await tokenService.saveToken(newTokenData);
        console.log('✅ Token refreshed successfully');
        return true; // Retry the request
      }
    } catch (error) {
      console.error('❌ Token refresh failed:', error);
      await handleAuthenticationFailure();
    }
  }
  return false; // Don't retry
};

// Helper: Clear tokens and redirect to login
const handleAuthenticationFailure = async () => {
  await tokenService.clearTokens();
  if (typeof window !== 'undefined') {
    window.location.href = '/login';
  }
};
```

### Step 3: Initialize in Your App

```typescript
// App.tsx (React/React Native)
import { setupRepositoryServices } from './config/repositoryConfig';

// Call once at app startup
setupRepositoryServices();

export default function App() {
  return (
    <YourAppComponent />
  );
}
```

```typescript
// app/layout.tsx (Next.js App Router)
'use client';

import { useEffect } from 'react';
import { setupRepositoryServices } from '@/config/repositoryConfig';

export default function RootLayout({ children }) {
  useEffect(() => {
    setupRepositoryServices();
  }, []);

  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
```

---

## 📚 Create a Service Layer

Wrap the repository client with business logic:

```typescript
// services/orderService.ts
import { RepositoryServices } from "rystem.repository.client";
import { 
  Order, 
  OrderRaw, 
  OrderStatus,
  mapRawToOrder, 
  mapOrderToRaw 
} from "../types/order";

export class OrderService {
  private getRepository() {
    return RepositoryServices.Repository<OrderRaw, string>("Order");
  }

  // ============================================
  // CRUD OPERATIONS
  // ============================================

  async getOrder(orderId: string): Promise<Order | null> {
    try {
      const raw = await this.getRepository().get(orderId);
      return raw ? mapRawToOrder(raw) : null;
    } catch (error) {
      console.error('Error fetching order:', error);
      return null;
    }
  }

  async createOrder(order: Order): Promise<boolean> {
    try {
      const raw = mapOrderToRaw(order);
      const result = await this.getRepository().insert(order.id, raw);
      return result.isOk;
    } catch (error) {
      console.error('Error creating order:', error);
      return false;
    }
  }

  async updateOrder(order: Order): Promise<boolean> {
    try {
      const raw = mapOrderToRaw(order);
      const result = await this.getRepository().update(order.id, raw);
      return result.isOk;
    } catch (error) {
      console.error('Error updating order:', error);
      return false;
    }
  }

  async deleteOrder(orderId: string): Promise<boolean> {
    try {
      const result = await this.getRepository().delete(orderId);
      return result.isOk;
    } catch (error) {
      console.error('Error deleting order:', error);
      return false;
    }
  }

  async orderExists(orderId: string): Promise<boolean> {
    try {
      const result = await this.getRepository().exist(orderId);
      return result.isOk;
    } catch (error) {
      console.error('Error checking order existence:', error);
      return false;
    }
  }

  // ============================================
  // QUERY OPERATIONS
  // ============================================

  async getAllOrders(): Promise<Order[]> {
    try {
      const results = await this.getRepository().query().execute();
      return results.map(r => mapRawToOrder(r.value));
    } catch (error) {
      console.error('Error fetching orders:', error);
      return [];
    }
  }

  async getOrdersByCustomer(customerId: string): Promise<Order[]> {
    try {
      const results = await this.getRepository()
        .query()
        .filter(`x => x.c == "${customerId}"`)
        .execute();
      
      return results.map(r => mapRawToOrder(r.value));
    } catch (error) {
      console.error('Error fetching customer orders:', error);
      return [];
    }
  }

  async getOrdersByStatus(status: OrderStatus): Promise<Order[]> {
    try {
      const results = await this.getRepository()
        .query()
        .filter(`x => x.s == ${status}`)
        .execute();
      
      return results.map(r => mapRawToOrder(r.value));
    } catch (error) {
      console.error('Error fetching orders by status:', error);
      return [];
    }
  }

  async getRecentOrders(days: number = 7): Promise<Order[]> {
    try {
      const cutoffDate = new Date();
      cutoffDate.setDate(cutoffDate.getDate() - days);
      const isoDate = cutoffDate.toISOString();

      const results = await this.getRepository()
        .query()
        .filter(`x => x.d > "${isoDate}"`)
        .execute();
      
      return results.map(r => mapRawToOrder(r.value));
    } catch (error) {
      console.error('Error fetching recent orders:', error);
      return [];
    }
  }

  // Query with builder (type-safe)
  async getOrdersAboveAmount(minAmount: number): Promise<Order[]> {
    try {
      const results = await this.getRepository()
        .query()
        .where()
        .select(x => x.a)
        .greaterThanOrEqual(minAmount)
        .build()
        .orderBy(x => x.a) // Order by amount
        .execute();
      
      return results.map(r => mapRawToOrder(r.value));
    } catch (error) {
      console.error('Error fetching orders by amount:', error);
      return [];
    }
  }

  // ============================================
  // AGGREGATIONS
  // ============================================

  async countOrders(): Promise<number> {
    try {
      return await this.getRepository().query().count();
    } catch (error) {
      console.error('Error counting orders:', error);
      return 0;
    }
  }

  async countOrdersByStatus(status: OrderStatus): Promise<number> {
    try {
      return await this.getRepository()
        .query()
        .where()
        .select(x => x.s)
        .equal(status)
        .count();
    } catch (error) {
      console.error('Error counting orders by status:', error);
      return 0;
    }
  }

  async getTotalRevenue(): Promise<number> {
    try {
      return await this.getRepository()
        .query()
        .where()
        .select(x => x.s)
        .equal(OrderStatus.Delivered)
        .sum(x => x.a);
    } catch (error) {
      console.error('Error calculating revenue:', error);
      return 0;
    }
  }

  // ============================================
  // BATCH OPERATIONS
  // ============================================

  async bulkCreateOrders(orders: Order[]): Promise<void> {
    try {
      const batcher = this.getRepository().batch();
      
      orders.forEach(order => {
        const raw = mapOrderToRaw(order);
        batcher.addInsert(order.id, raw);
      });
      
      const results = await batcher.execute();
      
      results.forEach(result => {
        if (!result.state.isOk) {
          console.error(`Failed to create order ${result.key}:`, result.state.message);
        }
      });
    } catch (error) {
      console.error('Error in bulk create:', error);
    }
  }

  async bulkUpdateOrders(orders: Order[]): Promise<void> {
    try {
      const batcher = this.getRepository().batch();
      
      orders.forEach(order => {
        const raw = mapOrderToRaw(order);
        batcher.addUpdate(order.id, raw);
      });
      
      await batcher.execute();
    } catch (error) {
      console.error('Error in bulk update:', error);
    }
  }

  async bulkDeleteOrders(orderIds: string[]): Promise<void> {
    try {
      const batcher = this.getRepository().batch();
      
      orderIds.forEach(id => {
        batcher.addDelete(id);
      });
      
      await batcher.execute();
    } catch (error) {
      console.error('Error in bulk delete:', error);
    }
  }
}

// Export singleton instance
export const orderService = new OrderService();
```

---

## 🎣 Create React Hooks

Make it easy to use in React components:

```typescript
// hooks/useOrder.ts
import { useState, useEffect, useCallback } from 'react';
import { orderService } from '../services/orderService';
import { Order, OrderStatus } from '../types/order';

export interface UseOrderOptions {
  refreshInterval?: number;
  skip?: boolean;
}

export interface UseOrderResult {
  order: Order | null;
  loading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
}

export const useOrder = (
  orderId: string | null,
  options: UseOrderOptions = {}
): UseOrderResult => {
  const { refreshInterval = 0, skip = false } = options;
  const [order, setOrder] = useState<Order | null>(null);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const fetchOrder = useCallback(async (silent = false) => {
    if (skip || !orderId) {
      setOrder(null);
      setError(null);
      return;
    }

    if (!silent) {
      setLoading(true);
    }
    setError(null);

    try {
      const fetchedOrder = await orderService.getOrder(orderId);
      setOrder(fetchedOrder);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch order';
      setError(message);
      console.error('Error fetching order:', err);
    } finally {
      if (!silent) {
        setLoading(false);
      }
    }
  }, [orderId, skip]);

  useEffect(() => {
    fetchOrder(false);
  }, [fetchOrder]);

  // Auto-refresh
  useEffect(() => {
    if (refreshInterval > 0) {
      const interval = setInterval(() => fetchOrder(true), refreshInterval);
      return () => clearInterval(interval);
    }
  }, [fetchOrder, refreshInterval]);

  const refetch = useCallback(() => fetchOrder(false), [fetchOrder]);

  return { order, loading, error, refetch };
};

// Hook for list of orders
export interface UseOrdersResult {
  orders: Order[];
  loading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
}

export const useOrdersByCustomer = (
  customerId: string | null,
  options: UseOrderOptions = {}
): UseOrdersResult => {
  const { refreshInterval = 0, skip = false } = options;
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const fetchOrders = useCallback(async (silent = false) => {
    if (skip || !customerId) {
      setOrders([]);
      setError(null);
      return;
    }

    if (!silent) {
      setLoading(true);
    }
    setError(null);

    try {
      const fetchedOrders = await orderService.getOrdersByCustomer(customerId);
      setOrders(fetchedOrders);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch orders';
      setError(message);
      console.error('Error fetching orders:', err);
    } finally {
      if (!silent) {
        setLoading(false);
      }
    }
  }, [customerId, skip]);

  useEffect(() => {
    fetchOrders(false);
  }, [fetchOrders]);

  useEffect(() => {
    if (refreshInterval > 0) {
      const interval = setInterval(() => fetchOrders(true), refreshInterval);
      return () => clearInterval(interval);
    }
  }, [fetchOrders, refreshInterval]);

  const refetch = useCallback(() => fetchOrders(false), [fetchOrders]);

  return { orders, loading, error, refetch };
};

// Hook for orders by status
export const useOrdersByStatus = (
  status: OrderStatus | null,
  options: UseOrderOptions = {}
): UseOrdersResult => {
  const { refreshInterval = 0, skip = false } = options;
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const fetchOrders = useCallback(async (silent = false) => {
    if (skip || status === null) {
      setOrders([]);
      setError(null);
      return;
    }

    if (!silent) {
      setLoading(true);
    }
    setError(null);

    try {
      const fetchedOrders = await orderService.getOrdersByStatus(status);
      setOrders(fetchedOrders);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch orders';
      setError(message);
      console.error('Error fetching orders:', err);
    } finally {
      if (!silent) {
        setLoading(false);
      }
    }
  }, [status, skip]);

  useEffect(() => {
    fetchOrders(false);
  }, [fetchOrders]);

  useEffect(() => {
    if (refreshInterval > 0) {
      const interval = setInterval(() => fetchOrders(true), refreshInterval);
      return () => clearInterval(interval);
    }
  }, [fetchOrders, refreshInterval]);

  const refetch = useCallback(() => fetchOrders(false), [fetchOrders]);

  return { orders, loading, error, refetch };
};
```

---

## 💻 Usage in Components

### React Component Example

```typescript
// components/OrderDetail.tsx
import React from 'react';
import { useOrder } from '../hooks/useOrder';

interface OrderDetailProps {
  orderId: string;
}

export const OrderDetail: React.FC<OrderDetailProps> = ({ orderId }) => {
  const { order, loading, error, refetch } = useOrder(orderId, {
    refreshInterval: 30000 // Refresh every 30 seconds
  });

  if (loading) {
    return <div>Loading order...</div>;
  }

  if (error) {
    return (
      <div>
        <p>Error: {error}</p>
        <button onClick={refetch}>Retry</button>
      </div>
    );
  }

  if (!order) {
    return <div>Order not found</div>;
  }

  return (
    <div>
      <h2>Order {order.orderNumber}</h2>
      <p>Status: {OrderStatus[order.status]}</p>
      <p>Customer: {order.customerId}</p>
      <p>Amount: ${order.totalAmount.toFixed(2)}</p>
      <p>Created: {order.createdAt.toLocaleDateString()}</p>
      <button onClick={refetch}>Refresh</button>
    </div>
  );
};
```

### React Native Component Example

```typescript
// screens/OrderListScreen.tsx
import React from 'react';
import { View, Text, FlatList, RefreshControl } from 'react-native';
import { useOrdersByStatus } from '../hooks/useOrder';
import { OrderStatus } from '../types/order';

export const OrderListScreen = () => {
  const { orders, loading, error, refetch } = useOrdersByStatus(
    OrderStatus.Pending
  );

  return (
    <View style={{ flex: 1 }}>
      <FlatList
        data={orders}
        keyExtractor={item => item.id}
        renderItem={({ item }) => (
          <View style={{ padding: 16, borderBottomWidth: 1 }}>
            <Text style={{ fontWeight: 'bold' }}>{item.orderNumber}</Text>
            <Text>${item.totalAmount.toFixed(2)}</Text>
          </View>
        )}
        refreshControl={
          <RefreshControl refreshing={loading} onRefresh={refetch} />
        }
        ListEmptyComponent={
          <Text style={{ textAlign: 'center', marginTop: 20 }}>
            {error ? `Error: ${error}` : 'No pending orders'}
          </Text>
        }
      />
    </View>
  );
};
```

---

## 🔐 Authentication Integration

### Token Storage Service

```typescript
// services/tokenService.ts

export interface TokenData {
  accessToken: string;
  refreshToken: string;
  expiresAt: number; // Timestamp in milliseconds
}

class TokenService {
  private TOKEN_KEY = 'auth_token';

  async saveToken(tokenData: TokenData): Promise<void> {
    try {
      // For web
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem(this.TOKEN_KEY, JSON.stringify(tokenData));
      }
      // For React Native - use AsyncStorage
      // await AsyncStorage.setItem(this.TOKEN_KEY, JSON.stringify(tokenData));
    } catch (error) {
      console.error('Error saving token:', error);
    }
  }

  async getToken(): Promise<TokenData | null> {
    try {
      // For web
      if (typeof localStorage !== 'undefined') {
        const data = localStorage.getItem(this.TOKEN_KEY);
        return data ? JSON.parse(data) : null;
      }
      // For React Native
      // const data = await AsyncStorage.getItem(this.TOKEN_KEY);
      // return data ? JSON.parse(data) : null;
      return null;
    } catch (error) {
      console.error('Error getting token:', error);
      return null;
    }
  }

  async getValidToken(): Promise<string | null> {
    const tokenData = await this.getToken();
    
    if (!tokenData) {
      return null;
    }

    // Check if token is expired (with 5-minute buffer)
    const now = Date.now();
    const buffer = 5 * 60 * 1000; // 5 minutes
    
    if (tokenData.expiresAt - buffer < now) {
      console.log('Token expired or about to expire, needs refresh');
      return null;
    }

    return tokenData.accessToken;
  }

  async getRefreshToken(): Promise<string | null> {
    const tokenData = await this.getToken();
    return tokenData?.refreshToken || null;
  }

  async clearTokens(): Promise<void> {
    try {
      // For web
      if (typeof localStorage !== 'undefined') {
        localStorage.removeItem(this.TOKEN_KEY);
      }
      // For React Native
      // await AsyncStorage.removeItem(this.TOKEN_KEY);
    } catch (error) {
      console.error('Error clearing tokens:', error);
    }
  }
}

export const tokenService = new TokenService();
```

### Auth Service (with Rystem.Authentication.Social)

```typescript
// services/authService.ts

export interface LoginResponse {
  tokenType: string;
  accessToken: string;
  refreshToken: string;
  expiresIn: number; // Seconds
}

export interface UserInfo {
  id: string;
  email: string;
  name: string;
}

class AuthService {
  private apiUrl = process.env.NEXT_PUBLIC_API_URL || "https://api.myapp.com";

  async login(email: string, password: string): Promise<LoginResponse> {
    const response = await fetch(`${this.apiUrl}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });

    if (!response.ok) {
      throw new Error('Login failed');
    }

    return await response.json();
  }

  async refreshToken(refreshToken: string): Promise<LoginResponse> {
    const response = await fetch(`${this.apiUrl}/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken })
    });

    if (!response.ok) {
      throw new Error('Token refresh failed');
    }

    return await response.json();
  }

  async getUserInfo(accessToken: string): Promise<UserInfo | null> {
    const response = await fetch(`${this.apiUrl}/auth/userinfo`, {
      headers: { 
        'Authorization': `Bearer ${accessToken}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      return null;
    }

    return await response.json();
  }

  async logout(): Promise<void> {
    await tokenService.clearTokens();
  }
}

export const authService = new AuthService();
```

---

## 🔧 Advanced Configuration

### Multiple Repositories with Different URLs

```typescript
RepositoryServices
  .Create("https://api.myapp.com/api/")
  .addRepository<OrderRaw, string>(x => {
    x.name = "Order";
    x.path = "Order"; // Full URL: https://api.myapp.com/api/Order
    x.addHeadersEnricher(() => getAuthHeaders());
  })
  .addRepository<ProductRaw, string>(x => {
    x.name = "Product";
    x.uri = "https://catalog-api.myapp.com/api/Product"; // Override full URL
    x.addHeadersEnricher(() => getAuthHeaders());
  });
```

### CQRS Repositories

```typescript
RepositoryServices
  .Create("https://api.myapp.com/api/")
  // Write operations to primary database
  .addCommand<OrderRaw, string>(x => {
    x.name = "OrderCommand";
    x.path = "Order";
    x.addHeadersEnricher(() => getAuthHeaders());
  })
  // Read operations from read replica
  .addQuery<OrderRaw, string>(x => {
    x.name = "OrderQuery";
    x.uri = "https://read-api.myapp.com/api/Order";
    x.addHeadersEnricher(() => getAuthHeaders());
  });

// Usage in service
const command = RepositoryServices.Command<OrderRaw, string>("OrderCommand");
const query = RepositoryServices.Query<OrderRaw, string>("OrderQuery");

await command.insert(orderId, orderRaw); // Write
const order = await query.get(orderId);   // Read
```

### Multiple Header Enrichers

```typescript
.addRepository<OrderRaw, string>(x => {
  x.name = "Order";
  x.path = "Order";
  
  // Add Authorization header
  x.addHeadersEnricher(async () => {
    const token = await tokenService.getValidToken();
    return { "Authorization": `Bearer ${token}` };
  });
  
  // Add custom tracking header
  x.addHeadersEnricher(async () => {
    return { "X-Request-Id": crypto.randomUUID() };
  });
  
  // Add tenant header (for multi-tenant apps)
  x.addHeadersEnricher(async () => {
    const tenantId = await getTenantId();
    return { "X-Tenant-Id": tenantId };
  });
})
```

### Custom Error Handlers

```typescript
.addRepository<OrderRaw, string>(x => {
  x.name = "Order";
  x.path = "Order";
  
  // Handle 401 Unauthorized
  x.addErrorHandler(async (endpoint, uri, method, headers, body, err) => {
    if (err.status === 401) {
      const refreshed = await refreshAuthToken();
      return refreshed; // true = retry, false = stop
    }
    return false;
  });
  
  // Handle 429 Rate Limit
  x.addErrorHandler(async (endpoint, uri, method, headers, body, err) => {
    if (err.status === 429) {
      const retryAfter = err.headers?.['retry-after'] || 60;
      console.log(`Rate limited. Retrying after ${retryAfter}s`);
      await new Promise(resolve => setTimeout(resolve, retryAfter * 1000));
      return true; // Retry
    }
    return false;
  });
  
  // Handle network errors
  x.addErrorHandler(async (endpoint, uri, method, headers, body, err) => {
    if (err.message?.includes('network')) {
      console.log('Network error detected, retrying...');
      await new Promise(resolve => setTimeout(resolve, 2000));
      return true; // Retry after 2 seconds
    }
    return false;
  });
})
```

---

## 🎯 Best Practices

### 1. Always Map Raw to Clean Types

```typescript
// ✅ GOOD - Clean separation
export interface OrderRaw { i: string; n: string; a: number; }
export interface Order { id: string; orderNumber: string; amount: number; }
export const mapRawToOrder = (raw: OrderRaw): Order => ({ ... });

// ❌ BAD - Using raw types directly in UI
const order: OrderRaw = await repository.get(id);
console.log(order.i); // What is 'i'?
```

### 2. Use Service Layer

```typescript
// ✅ GOOD - Business logic in service
class OrderService {
  async getActiveOrders() {
    const orders = await this.getOrdersByStatus(OrderStatus.Pending);
    return orders.filter(o => o.totalAmount > 0);
  }
}

// ❌ BAD - Business logic in component
const orders = await RepositoryServices.Repository<OrderRaw, string>("Order")
  .query().execute();
const activeOrders = orders.filter(...); // Logic in component!
```

### 3. Use Hooks for React

```typescript
// ✅ GOOD - Reusable hook
const { order, loading, error } = useOrder(orderId);

// ❌ BAD - Direct service call in component
const [order, setOrder] = useState(null);
useEffect(() => {
  orderService.getOrder(orderId).then(setOrder);
}, [orderId]);
```

### 4. Handle Errors Gracefully

```typescript
// ✅ GOOD - Error handling
try {
  const order = await orderService.getOrder(orderId);
  if (!order) {
    showToast('Order not found');
    return;
  }
  // Process order
} catch (error) {
  console.error('Error:', error);
  showToast('Failed to load order');
}

// ❌ BAD - No error handling
const order = await orderService.getOrder(orderId);
processOrder(order); // Will crash if null!
```

### 5. Use TypeScript Generics Correctly

```typescript
// ✅ GOOD - Correct generic types
RepositoryServices.Repository<OrderRaw, string>("Order")
//                             ^entity  ^key

// ❌ BAD - Wrong types
RepositoryServices.Repository<Order, number>("Order")
// Should use OrderRaw (raw interface) and string (key type)
```

---

## ⚠️ Important Notes

1. **Always use Raw types** with Repository client (matching C# JsonPropertyName)
2. **Map to clean types** in your service layer for use in UI
3. **Configure once** at app startup with `setupRepositoryServices()`
4. **Error handlers run in order** - first one returning `true` stops the chain
5. **Header enrichers are called** for every request
6. **CQRS requires separate** `addCommand` and `addQuery` calls
7. **Package name**: `rystem.repository.client` (npm)

---

## 🔗 Related Resources

- **repository-api-server**: How to expose repositories as REST APIs on backend
- **repository-api-client-dotnet**: Client for .NET/C# apps (Blazor, MAUI, WPF)
- **repository-setup**: How to configure repositories on backend
- **auth-flow**: Setting up authentication with Rystem.Authentication.Social

---

## 📖 Further Reading

- [rystem.repository.client GitHub](https://github.com/KeyserDSoze/RepositoryFramework/tree/master/src/RepositoryFramework.Api.Client)
- [TypeScript Client Examples](https://github.com/KeyserDSoze/RepositoryFramework/tree/master/src/RepositoryFramework.Test/RepositoryFramework.Api.Client.Test)

---

## ✅ Summary

**rystem.repository.client** provides:
- ✅ Type-safe repository access from TypeScript/JavaScript
- ✅ Automatic authentication with token refresh
- ✅ Custom error handlers with retry logic
- ✅ LINQ-style queries from client-side
- ✅ Batch operations support
- ✅ CQRS pattern support
- ✅ Works in React, React Native, Next.js, Node.js
- ✅ Multiple header enrichers
- ✅ Custom error handling per repository

**Use this tool to build type-safe, production-ready frontend applications!** 🚀
