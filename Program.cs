using Azure.Core.Diagnostics;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.Diagnostics.Tracing;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

using var traceLogger = AzureEventSourceListener.CreateTraceLogger(EventLevel.Verbose);
using var consoleLogger = AzureEventSourceListener.CreateConsoleLogger(EventLevel.Verbose);

// Configure Azure App Configuration and Key Vault services using the same DefaultAzureCredential.
DefaultAzureCredential credential = new(new DefaultAzureCredentialOptions()
{
    Diagnostics =
    {
        LoggedQueryParameters = { "resource" },
    },

    // The following is only to mitigate my development machine supplying the wrong SP and should not affect the repro.
    ExcludeEnvironmentCredential = true,
});

var endpoint = builder.Configuration["APPCONFIG_URI"];
if (Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options
            .Connect(endpointUri, credential)
            .ConfigureClientOptions(configure =>
            {
                configure.Diagnostics.LoggedHeaderNames.Add("WWW-Authenticate");
            })
            .Select(Microsoft.Extensions.Configuration.AzureAppConfiguration.KeyFilter.Any);
    });
}

endpoint = builder.Configuration["KEYVAULT_URI"];
if (Uri.TryCreate(endpoint, UriKind.Absolute, out endpointUri))
{
    SecretClient secretClient = new(endpointUri, credential, new()
    {
        Diagnostics =
    {
        LoggedHeaderNames = { "WWW-Authenticate" },
    }
    });
    builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());

    builder.Services.AddSingleton(secretClient);
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
