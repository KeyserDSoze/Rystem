# Storage Services Architecture

## 📐 Pattern: Infrastructure Decorator

This implementation follows the **Decorator Pattern** with clear separation between **Infrastructure** (generic storage) and **Domain Logic** (business-specific behavior).

```
┌─────────────────────────────────────┐
│  Domain Services (Decorators)       │
│  - PkceStorageService               │  ← Adds PKCE-specific logic (key prefixes, validation)
│  - TokenStorageService              │  ← Adds token-specific logic (expiry check, JSON)
└──────────────┬──────────────────────┘
               │ uses (dependency injection)
               ↓
┌─────────────────────────────────────┐
│  Interface (Port/Contract)          │
│  - IStorageService                  │  ← Generic key-value storage abstraction
└──────────────┬──────────────────────┘
               │ implemented by
               ↓
┌─────────────────────────────────────┐
│  Infrastructure (Adapters)          │
│  - LocalStorageService (default)    │  ← Browser localStorage
│  - SecureStorageService (mobile)    │  ← iOS Keychain / Android KeyStore (TODO)
│  - MockStorageService (test)        │  ← In-memory for testing (TODO)
└─────────────────────────────────────┘
```

---

## 🔧 Components

### 1. **IStorageService** (Interface)
Generic key-value storage interface. Allows swapping infrastructure implementations without changing domain logic.

```typescript
interface IStorageService {
    get(key: string): string | null;
    set(key: string, value: string): void;
    remove(key: string): void;
    has(key: string): boolean;
    clear?(): void;
}
```

**Example Implementations:**
- `LocalStorageService` - Browser `localStorage` (default)
- `SecureStorageService` - Mobile secure storage (React Native/MAUI)
- `MockStorageService` - In-memory for testing

---

### 2. **LocalStorageService** (Default Implementation)
Default adapter for web browsers using `localStorage`.

```typescript
const storage = new LocalStorageService();
storage.set('key', 'value');
storage.get('key'); // 'value'
```

**Features:**
- ✅ Persistent storage (survives browser refresh)
- ✅ Error handling with console warnings
- ✅ Works in all modern browsers
- ❌ Not suitable for sensitive data on mobile

---

### 3. **PkceStorageService** (Decorator)
Domain service for PKCE (Proof Key for Code Exchange) OAuth 2.0 flows.

```typescript
const storage = new LocalStorageService();
const pkceService = new PkceStorageService(storage);

// Store PKCE values
pkceService.storeCodeVerifier('microsoft', codeVerifier);
pkceService.storeCodeChallenge('microsoft', codeChallenge);

// Retrieve (with validation)
const verifier = pkceService.getCodeVerifier('microsoft');

// Consume once (get and remove)
const verifier = pkceService.getAndRemoveCodeVerifier('microsoft');

// Clear all PKCE data for provider
pkceService.clearAll('microsoft');
```

**Features:**
- ✅ Automatic key prefixing (`rystem_pkce_microsoft_verifier`)
- ✅ Validation (PKCE verifier must be 43+ chars, RFC 7636)
- ✅ Separate storage for `verifier` and `challenge`
- ✅ `getAndRemoveCodeVerifier()` for atomic consume

**When to Clear:**
- ✅ After **successful** token exchange
- ✅ After **failed** token exchange (to allow retry)
- ❌ **Never** on component mount (breaks Redirect flow!)

---

### 4. **TokenStorageService** (Decorator)
Domain service for social authentication tokens with expiry management.

```typescript
const storage = new LocalStorageService();
const tokenService = new TokenStorageService(storage);

// Save token with expiry
tokenService.saveToken({
    accessToken: 'eyJ...',
    refreshToken: 'refresh...',
    expiresAt: new Date('2024-12-31T23:59:59Z')
});

// Get token (returns null if expired)
const token = tokenService.getToken();

// Check if token exists and valid
if (tokenService.hasToken()) {
    // Token is valid
}

// Clear token
tokenService.clearToken();
```

**Features:**
- ✅ Automatic expiry check (returns `null` if expired)
- ✅ JSON serialization/deserialization
- ✅ Separate storage for token + expiry timestamp
- ✅ Auto-cleanup on expiry

---

## 🚀 Usage

### Basic Setup (Web App)

```typescript
import { setupSocialLogin, LocalStorageService } from 'rystem.authentication.social.react';

setupSocialLogin(x => {
    x.apiUri = "https://api.yourdomain.com";
    
    // Default: LocalStorageService (automatically initialized)
    // x.storageService = new LocalStorageService();  // No need to set explicitly
    
    x.microsoft.clientId = "your-client-id";
});
```

### Custom Storage (Mobile Secure Storage)

```typescript
import { setupSocialLogin, IStorageService } from 'rystem.authentication.social.react';

// Custom implementation for React Native secure storage
class SecureStorageService implements IStorageService {
    get(key: string): string | null {
        // Use react-native-keychain or similar
        return SecureStore.getItemAsync(key);
    }
    
    set(key: string, value: string): void {
        SecureStore.setItemAsync(key, value);
    }
    
    remove(key: string): void {
        SecureStore.deleteItemAsync(key);
    }
    
    has(key: string): boolean {
        return SecureStore.getItemAsync(key) !== null;
    }
}

setupSocialLogin(x => {
    x.storageService = new SecureStorageService();  // Override default
    // ...
});
```

### Testing with Mock Storage

```typescript
class MockStorageService implements IStorageService {
    private storage = new Map<string, string>();
    
    get(key: string) { return this.storage.get(key) ?? null; }
    set(key: string, value: string) { this.storage.set(key, value); }
    remove(key: string) { this.storage.delete(key); }
    has(key: string) { return this.storage.has(key); }
    clear() { this.storage.clear(); }
}

setupSocialLogin(x => {
    x.storageService = new MockStorageService();  // For testing
});
```

---

## 🔐 PKCE Flow (Redirect Mode Fix)

### ❌ Old Behavior (Broken)
```typescript
useEffect(() => {
    clearCodeVerifier('microsoft');  // ❌ Deletes PKCE on every mount!
    const verifier = generateCodeVerifier();
    storeCodeVerifier('microsoft', verifier);
}, []);
```

**Problem:** With **Redirect Mode**:
1. User clicks button → PKCE generated and stored
2. **Browser redirects** to Microsoft OAuth
3. Microsoft redirects back → **React remounts component**
4. `useEffect` runs → **PKCE deleted!** ❌
5. Token exchange fails (no matching `code_verifier`)

### ✅ New Behavior (Fixed)
```typescript
const pkceStorage = new PkceStorageService(settings.storageService);

useEffect(() => {
    const existingVerifier = pkceStorage.getCodeVerifier('microsoft');
    
    if (existingVerifier) {
        // ✅ Redirect flow: Reuse existing PKCE
        console.log('Reusing PKCE (returning from OAuth redirect)');
        // Rebuild OAuth URL with existing challenge
    } else {
        // ✅ Fresh flow: Generate new PKCE
        const verifier = generateCodeVerifier();
        pkceStorage.storeCodeVerifier('microsoft', verifier);
    }
}, []);
```

**Cleanup Strategy:**
- ✅ Clear PKCE **after successful token exchange** (`SocialLoginManager.updateToken`)
- ✅ Clear PKCE **after failed token exchange** (allows retry with new PKCE)
- ❌ **Never clear** PKCE on component mount (breaks redirect flow)

---

## 📖 Architecture Benefits

### 1. **Separation of Concerns**
- **Infrastructure**: Generic storage (how to persist)
- **Domain Logic**: Business rules (what to persist, when to validate)

### 2. **Testability**
```typescript
// Easy to mock for testing
const mockStorage = new MockStorageService();
const pkceService = new PkceStorageService(mockStorage);

// Test without browser localStorage
expect(pkceService.getCodeVerifier('test')).toBeNull();
```

### 3. **Flexibility**
- Web: `LocalStorageService` (browser `localStorage`)
- Mobile: `SecureStorageService` (iOS Keychain, Android KeyStore)
- Server: `RedisStorageService` (remote caching)
- Test: `MockStorageService` (in-memory)

### 4. **Security**
- Mobile apps can use secure storage (encrypted)
- Web apps can use `localStorage` (acceptable for non-sensitive data)
- Easy to add encryption layer without changing domain services

---

## 🔄 Migration from Old Code

### Before (Direct localStorage)
```typescript
// Old: Direct localStorage access (scattered throughout code)
sessionStorage.setItem('microsoft_code_verifier', verifier);
const verifier = sessionStorage.getItem('microsoft_code_verifier');
sessionStorage.removeItem('microsoft_code_verifier');
```

### After (Abstracted Service)
```typescript
// New: Abstracted storage service (centralized, testable)
const pkceStorage = new PkceStorageService(settings.storageService);
pkceStorage.storeCodeVerifier('microsoft', verifier);
const verifier = pkceStorage.getCodeVerifier('microsoft');
pkceStorage.removeCodeVerifier('microsoft');
```

**Benefits:**
- ✅ Centralized storage logic
- ✅ Easy to swap infrastructure (localStorage → secure storage)
- ✅ Automatic validation (PKCE length check)
- ✅ Consistent key naming (`rystem_pkce_microsoft_verifier`)

---

## 🛠️ Future Enhancements

### 1. Secure Storage for Mobile
```typescript
// React Native with expo-secure-store
import * as SecureStore from 'expo-secure-store';

class ExpoSecureStorageService implements IStorageService {
    async get(key: string): Promise<string | null> {
        return await SecureStore.getItemAsync(key);
    }
    // ...
}
```

### 2. Redis for Server-Side Rendering
```typescript
class RedisStorageService implements IStorageService {
    constructor(private redis: RedisClient) {}
    
    async get(key: string): Promise<string | null> {
        return await this.redis.get(key);
    }
    // ...
}
```

### 3. Encryption Layer
```typescript
class EncryptedStorageService implements IStorageService {
    constructor(
        private baseStorage: IStorageService,
        private encryptionKey: string
    ) {}
    
    set(key: string, value: string): void {
        const encrypted = encrypt(value, this.encryptionKey);
        this.baseStorage.set(key, encrypted);
    }
    // ...
}
```

---

## 📚 Related Documentation

- **PKCE RFC 7636**: https://tools.ietf.org/html/rfc7636
- **OAuth 2.0 Security Best Practices**: https://tools.ietf.org/html/draft-ietf-oauth-security-topics
- **Decorator Pattern**: https://refactoring.guru/design-patterns/decorator
- **Dependency Injection**: https://en.wikipedia.org/wiki/Dependency_injection
