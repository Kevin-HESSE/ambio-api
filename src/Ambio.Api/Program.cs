using Ambio.Api.JobApplications;
using Ambio.Application;
using Ambio.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

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

app.UseHttpsRedirection();

app.MapJobApplicationEndpoints();

app.Run();
