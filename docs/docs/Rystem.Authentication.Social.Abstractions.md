# Rystem.Authentication.Social.Abstractions
This NuGet package provides utility for social authentication and includes the following class: `TokenResponse`.

## Dependencies
The package requires the following libraries:
- Google.Apis.Auth (minimum version: 1.68.0)
- Microsoft.IdentityModel.Protocols.OpenIdConnect (minimum version: 8.1.2)
- Microsoft.IdentityModel.Tokens (minimum version: 8.1.2)
- System.IdentityModel.Tokens.Jwt (minimum version: 8.1.2)
- Rystem.DependencyInjection (minimum version: 6.2.0)

## Classes

### TokenResponse
This class represents a token response from the social authentication process.

#### Properties:

- **Username**: Contains the unique string identifier (username) of a user.
  - **Type**: `string`
  - **Usage**: This property is used to identify a user in your application.
  
    ```csharp
    var tokenResponse = new TokenResponse();
    tokenResponse.Username = "user1";
    ```

- **Claims**: Contains user claims obtained during the authentication process.
  - **Type**: `List<Claim>`
  - **Usage**: This property is used to store user claims that can be used in your application for access control or personalization.

    ```csharp
    var tokenResponse = new TokenResponse();
    tokenResponse.Claims = new List<Claim>{new Claim("Role", "Admin")};
    ```

#### Static Properties:

- **Empty**: Represents an empty or non-existent `TokenResponse` object.
  - **Type**: `TokenResponse`
  - **Usage**: This property can be used to represent the absence of a valid token response.

    ```csharp
    var tokenResponse = TokenResponse.Empty;
    ```

This class does not have any public methods. This class can be used to extract identity information from the user's social account after they've been authenticated. It encapsulates the response for easy consumption within your application. Please use the documented properties to manipulate and access this response.