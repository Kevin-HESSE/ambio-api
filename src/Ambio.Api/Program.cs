using System.Text.Json;
using System.Text.Json.Serialization;

using Ambio.Api.JobApplications;
using Ambio.Application;
using Ambio.Data;

using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer((operation, _, _) =>
    {
        foreach (var parameter in operation.Parameters ?? [])
        {
            if (parameter is OpenApiParameter { In: ParameterLocation.Query, Name: not null } queryParameter)
            {
                queryParameter.Name = JsonNamingPolicy.CamelCase.ConvertName(queryParameter.Name);
            }
        }

        return Task.CompletedTask;
    });
});
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddAmbioData(
    migrationAssembly: "Ambio.Migrations",
    connectionString: builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddAmbioApplication();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

// Gives a ProblemDetails body to empty error responses, such as a query parameter that cannot be parsed
app.UseStatusCodePages();

app.UseHttpsRedirection();

app.MapJobApplicationEndpoints();

app.Run();
