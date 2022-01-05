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

using var listener = AzureEventSourceListener.CreateTraceLogger(EventLevel.Verbose);

// Configure Azure App Configuration and Key Vault services using the same DefaultAzureCredential.
DefaultAzureCredential credential = new(new DefaultAzureCredentialOptions()
{
    Diagnostics =
    {
        IsLoggingContentEnabled = builder.Environment.IsDevelopment(),
    },
});

//Uri appConfigUri = new(builder.Configuration["APPCONFIG_URI"]);
//builder.Configuration.AddAzureAppConfiguration(options =>
//{
//    options
//        .Connect(appConfigUri, credential)
//        .Select(Microsoft.Extensions.Configuration.AzureAppConfiguration.KeyFilter.Any);
//});

Uri keyVaultUri = new(builder.Configuration["KEYVAULT_URI"]);
SecretClient secretClient = new(keyVaultUri, credential);
builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
builder.Services.AddSingleton(secretClient);

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
