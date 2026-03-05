### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Add to service collection the multiple localization from different sources/libraries

You have to add a service for localization with Multiple word.
From library 1 for example

    services.AddMultipleLocalization<Shared1>(x =>
        {
            x.ResourcesPath = "Resources";
        });

and for library 2

    services.AddMultipleLocalization<Shared2>(x =>
        {
            x.ResourcesPath = "Resources";
        });

When you use the IStringLocalizer<T> the model T will find the correct assembly resources, choosing between assembly 1 (from Shared1) or assembly 2 (from Shared2).

Inject a model from library 1 to access to resources from library 1, and the same for library 2.

    IStringLocalizer<SameClassInShared1Library1>
    IStringLocalizer<SameClassInShared1Library2>
