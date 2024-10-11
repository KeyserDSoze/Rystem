## Class: ServiceCollectionExtensions
This class provides extensions to the `IServiceCollection` for implementing social login in Blazor application.

### Method: AddSocialLoginUI
**Purpose**: This method is used to configure and register all the required services and settings to the `IServiceCollection` for supporting social login.

**Parameters**: 
- IServiceCollection `services`: The `IServiceCollection` which is a contract for a collection of service descriptors.
- Action<SocialLoginAppSettings> `settings`: An action delegate for configuring the `SocialLoginAppSettings`.

**Return Value**: It returns the service collection after adding all the required services and settings.

**Usage Example**:
```csharp
services.AddSocialLoginUI(settings =>
{
    settings.ApiUrl = "http://myapiurl.com";
});
```

## Class: SocialLoginAppSettings
This class represents settings/configuration for social login like google, facebook, microsoft.

### Properties:
- string? `ApiUrl`: The API URL.
- SocialParameter `Google`: Settings for Google login.
- SocialParameter `Facebook`: Settings for Facebook login.
- SocialParameter `Microsoft`: Settings for Microsoft login.

## Class: SocialParameter
This class represents settings for individual social providers.

### Properties:
- string? `ClientId`: The Client Id for the social provider.

## Class: SocialUserWrapper
This class represents a wrapper containing user details with associated logout method.

### Properties:
- TUser `User`: The user details of type TUser, adhering to ISocialUser.
- string `CurrentToken`: The current token.
- SocialLogout `LogoutAsync`: Delegate representing the logout method.

## Class: State
This class represents the state of a login session.

**Properties**:
- SocialLoginProvider `Provider`: The social login provider.
- string `Value`: The state value.
- DateTime `ExpiringTime`: The expiration time of the state.
- string `Path`: The request path.

## Class: Token
This class represents the token object received from the social login service.

**Properties**:
- string `AccessToken`: The access token.
- string `RefreshToken`: The refresh token.
- bool `IsExpired`: Indicator whether the token is expired.
- long `ExpiresIn`: The expiration duration of the token.
- DateTime `Expiring`: The specific time at which the token will expire.

## Class: SocialLoginAuthorizationHeaderProvider
This class represents a provider for creating authorization header.

### Method: CreateAuthorizationHeaderAsync, CreateAuthorizationHeaderForUserAsync, CreateAuthorizationHeaderForAppAsync
**Purpose**: These methods are used to generate an authorization header based on the current state of the authentication session.

**Parameters**: 
- IEnumerable<string> `scopes`: The scopes for which the authorization header is required.
- AuthorizationHeaderProviderOptions? `options`: Optional parameter for additional options for generating the header.
- ClaimsPrincipal? `claimsPrincipal`: The Claims Principal.

**Return Value**: It returns a string representing the authorization header.

## Class: SocialLoginLocalStorageService
This class provides methods to interact with the browser's local storage to manage authentication state.

### Method: GetTokenAsync
**Purpose**: This method fetches the token from the local storage.
**Return Value**: It returns a `Token` object or `null` if no token is found.

### Method: SetTokenAsync
**Purpose**: This method sets a `Token` in the local storage.
**Parameters**: 
- Token `token`: The token object to be stored in local storage.

### Method: GetStateAsync
**Purpose**: This method fetches the login state from the local storage.
**Return Value**: It returns a `State` object or `null` if no state is found.

### Method: SetStateAsync
**Purpose**: This method sets the login state to the local storage.
**Parameters**: 
- SocialLoginProvider `provider`: The social login provider.

**Return Value**: It returns a `string` representing the state value.

### Delete Methods: DeleteStateAsync, DeleteTokenAsync
**Purpose**: These methods delete the state or token from the local storage.

## Class: SocialLoginManager
This class represents a manager for handling operations like fetching token, logging out, getting user details.

### Method: GetRedirectUri
**Purpose**: This method gets the base URI for directing the user for login.
**Return Value**: It returns a `string` representing the base URL.

### Method: MeAsync
**Purpose**: This method fetches the user details from the social login provider.
**Return Value**: It returns a `SocialUserWrapper<TUser>` object representing the user details.

### Method: FetchTokenAsync
**Purpose**: This method fetches the token either from the local storage, or by redirecting to the social provider's login page.
**Return Value**: It returns a `Token` object representing the authentication token.

### Method: LogoutAsync
**Purpose**: This method performs the logout operation by removing the user details and token from the local storage.

These methods and classes facilitate the use of social logins in Blazor applications by managing the login state, user details, and tokens.