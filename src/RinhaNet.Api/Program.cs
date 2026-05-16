using Microsoft.AspNetCore.Mvc;

namespace RinhaNet.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.MapGet("/ready", () => Results.Ok());
        app.MapPost("/fraud-score", ([FromBody] FraudScoreRequest request) => Results.Ok(request));
        app.Run();
    }
}
