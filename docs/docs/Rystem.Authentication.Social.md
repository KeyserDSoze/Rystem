# Class Documentation

## 1. Class: SocialLoginBuilder

This class serves as a configuration builder to set up different social login providers. It includes social login settings for Google, Microsoft, Facebook, Amazon, GitHub, LinkedIn, Instagram, Pinterest, and TikTok.

### - Method Name: N/A (Properties only)

### - Parameters: None

### - Return Value: N/A

### - Usage Example: 
```csharp
SocialLoginBuilder builder = new SocialLoginBuilder();
builder.Google.ClientId = "<your google app client id>";
builder.Google.ClientSecret = "<your google app client secret>";
builder.Google.RedirectDomain = "<your app redirect url for google login>";
```

## 2. Class: SocialDefaultLoginSettings

This abstraction provides a basic implementation for a social login setting, with an IsActive flag set to true by default.

### - Method Name: N/A (Properties only)

### - Parameters: None

### - Return Value: N/A

### - Usage Example: Not directly used, this class serves as a base for other social login settings.


## 3. Class: SocialLoginSettings

Inherits from `SocialDefaultLoginSettings` and used as a base class for other authentication classes, it includes `ClientId` for social authentication and overrides the IsActive property to make sure `ClientId` is not null.

### - Method Name: IsActive

An override property that returns true only if a ClientId is set.

### - Parameters: N/A

### - Return Value: A boolean value indicating if this object is correctly configured for social login.

### - Usage Example: Not directly used, this class serves as the base for other social login settings.

## 4. Class: SocialLoginWithRedirectSettings

This class inherits from `SocialLoginSettings` and extends it by adding a RedirectDomain property required for redirects in social login flows. The IsActive property is overridden again to make sure all new properties are set.

### - Method Name: IsActive

It checks whether `ClientId` and `RedirectDomain` are both set.

### - Parameters: N/A

### - Return Value: A boolean value indicating whether a ClientId and RedirectDomain is set or not, true if both are set, false otherwise.

### - Usage Example:
```csharp
SocialLoginWithRedirectSettings googleLoginSettings = new SocialLoginWithRedirectSettings();
googleLoginSettings.ClientId = "<your google app client id>";
googleLoginSettings.RedirectDomain = "<your app redirect url for google login>";
```


## 5. Class: SocialLoginWithSecretsSettings

This class inherits from `SocialLoginSettings` and extends it by adding a `ClientSecret` property required for social login flows. It overrides the `IsActive` property to make sure all the properties are set.

### - Method Name: IsActive

It checks whether `ClientId` and `ClientSecret` are set.

### - Parameters: N/A

### - Return Value: A boolean value indicating whether a ClientId and ClientSecret is set or not, true if both are set, false otherwise.

### - Usage Example:
```csharp
SocialLoginWithSecretsSettings githubLoginSettings = new SocialLoginWithSecretsSettings();
githubLoginSettings.ClientId = "<your github app client id>";
githubLoginSettings.ClientSecret = "<your github app client secret>";
```

# Test Class Documentation

Due to the provided information not containing specific test classes, additional information about edge cases or usage could not be generated. Kindly provide specific test classes for better in context information.