using Rystem.Authentication.Social.TestApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSocialLogin(x =>
{
    x.Google.ClientId = builder.Configuration["SocialLogin:Google:ClientId"];
    x.Google.ClientSecret = builder.Configuration["SocialLogin:Google:ClientSecret"];
    x.Google.RedirectDomain = builder.Configuration["SocialLogin:Google:RedirectDomain"];
    x.Microsoft.ClientId = builder.Configuration["SocialLogin:Microsoft:ClientId"];
    x.Microsoft.ClientSecret = builder.Configuration["SocialLogin:Microsoft:ClientSecret"];
    x.Microsoft.RedirectDomain = builder.Configuration["SocialLogin:Microsoft:RedirectDomain"];
    x.Facebook.ClientId = builder.Configuration["SocialLogin:Facebook:ClientId"];
    x.Facebook.ClientSecret = builder.Configuration["SocialLogin:Facebook:ClientSecret"];
    x.Facebook.RedirectDomain = builder.Configuration["SocialLogin:Facebook:RedirectDomain"];
},
x =>
{
    x.BearerTokenExpiration = TimeSpan.FromHours(1);
    x.RefreshTokenExpiration = TimeSpan.FromDays(10);
});
builder.Services.AddSocialUserProvider<SocialUserProvider>();
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(x =>
{
    x.AddPolicy("all", t =>
    {
        t.AllowAnyHeader().AllowAnyOrigin();
    });
});
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("all");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseSocialLoginEndpoints();

app.Run();
