using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Server.Controllers;
using Server.DbModels;
using Server.Services;
using Server.Services.Interfaces;
using Server.Shared;
using Server.Shared.Secu;
using Server.Shared.Secu.Swagger;

var builder = WebApplication.CreateBuilder(args);
var conf = builder.Configuration;
var env = builder.Environment;

// Add services to the container.
builder.WebHost.ConfigureKestrel(serverOptions => serverOptions.AddServerHeader = false);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ErrorHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddCors();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddConfiguration(conf.GetSection("Logging")));
builder.Services.AddApplicationInsightsTelemetry();

// Add DB
builder.Services.AddDbContext<StuffDbContext>(options => options.UseSqlite(
    conf.GetConnectionString("SqlConnectionString"),
    sqlServerOptions => sqlServerOptions.CommandTimeout(conf.GetSection("ConnectionStrings:SqlCommandTimeout").Get<int>()))
);

// Add Authent
builder.AddAuthentication(conf);
builder.AddAuthorization();
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery();

builder.Services.AddHsts(configureOptions =>
{
    configureOptions.Preload = true;
    configureOptions.IncludeSubDomains = true;
    configureOptions.MaxAge = TimeSpan.FromDays(365);
});

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    options.HttpsPort = 443;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    options.RequireHeaderSymmetry = false;
});

// Register the Swagger generator
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Server", Version = "v1" });
    c.OperationFilter<AddRequiredCsrfHeaderOperationFilter>();
    c.DocInclusionPredicate((_, apiDesc) => !apiDesc.RelativePath!.Equals("favicon.ico", StringComparison.Ordinal));
    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
    c.AddSecurityRequirement(d => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, d)] = []
    });
});

// Register Services
builder.Services.AddScoped<ITicketStore, DistributedCacheTicketStore>();
builder.Services.AddScoped<IBffTokensService, BffTokensService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStuffService, StuffService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
app.UseHttpHeaders();
app.UseExceptionHandler(_ => { });
app.UseHsts();
app.UseHttpsRedirection();
app.UseAuthCors(conf);
app.UseAuthentication();
app.UseAuthorization();
app.UseStoreAntiforgery();
app.UseAntiforgery();
app.UseLocalSpa(env, conf);
app.UseStaticFiles();
app.MapFallbackToFile("/index.html");
app.UseFileServer(new FileServerOptions
{
    EnableDirectoryBrowsing = false,
    EnableDefaultFiles = true,
    DefaultFilesOptions = { DefaultFileNames = { "index.html" } }
});
if (!env.IsProduction())
{
    app.UseSwaggerUI(c =>
    {
        c.DisplayRequestDuration();
        c.EnableTryItOutByDefault();
    });
}


app.MapRoutes();
app.MapAuthRoutes();
app.MapSwagger();
app.MapHealthChecks("/health").AllowAnonymous();

await app.RunAsync();

public partial class Program
{
    protected Program()
    {
    }
}
