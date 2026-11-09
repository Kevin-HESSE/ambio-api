using Ambio.Api.JobApplications;
using Ambio.Application;
using Ambio.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddAmbioContext(
    migrationAssembly: "Ambio.Migrations",
    connectionString: builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddAmbioServices();

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
