### [What is Rystem?](https://github.com/KeyserDSoze/RystemV3)

## Add to service collection the UI service in your blazor DI

You have to add a service for UI

    builder.Services
        .AddRepositoryUI();

and add the endpoint for your repository

    app
        .AddDefaultRepositoryEndpoints();


### Demand everything to the framework        

In the Host.cshtml you have to add style, javascript files and the RepositoryApp.

    <html>
        <head>
          <!-- inside of head section -->
          <partial name="RepositoryStyle" />
        </head>
        <body>
          <component type="typeof(RepositoryApp<App>)" render-mode="ServerPrerendered" />
          <!-- inside of body section and after the div/app tag  -->
          <partial name="RepositoryScript" />
        </body>
    </html>

Instead of "App" class you can use every class in your DLL, but remember the class needs to be inside your blazor/razor application.

### Demand everything to the framework with authentication

In the Host.cshtml you have to add style, javascript files and the RepositoryAuthenticatedApp.

    <html>
        <head>
          <!-- inside of head section -->
          <partial name="RepositoryStyle" />
        </head>
        <body>
          <component type="typeof(RepositoryAuthenticatedApp<App>)" render-mode="ServerPrerendered" />
          <!-- inside of body section and after the div/app tag  -->
          <partial name="RepositoryScript" />
        </body>
    </html>

Instead of "App" class you can use every class in your DLL, but remember the class needs to be inside your blazor/razor application.

### Use razor component instead to build your mixed custom repository UI

In the Host.cshtml you have to add style, javascript files
 
    <html>
        <head>
          <!-- inside of head section -->
          <partial name="RepositoryStyle" />
        </head>
        <body>
          <component type="typeof(App)" render-mode="ServerPrerendered" />
          <!-- inside of body section and after the div/app tag  -->
          <partial name="RepositoryScript" />
        </body>
    </html>

In your app you can use

Component          | Usage
------------------ | --------------------------------------------------
RepositoryManager  | A component to manage your repository in one page

    

