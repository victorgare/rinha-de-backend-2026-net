using Microsoft.AspNetCore.Mvc;
using RinhaNet.Api.Resources;
using RinhaNet.Api.VectorSearch.BruteForce;

namespace RinhaNet.Api;

public class Program
{
    public static async Task Main(string[] args)
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

        var searchEngine = new VectorSearchEngine("Data");
        app.MapGet("/ready", () => Results.Ok());
        app.MapPost("/fraud-score", async ([FromBody] FraudScoreRequest request) =>
        {
            const int topK = 5;
            var dimensions = GetDimensions(request);
            var result = await searchEngine.SearchAsync(dimensions, topK);

            var score = (float)result.Count(c => c.Label == "fraud") / topK;
            return Results.Ok(new
            {
                approved = score < 0.6,
                fraud_score = score,
            });
        });

        await app.RunAsync();
    }

    private static float[] GetDimensions(FraudScoreRequest request)
    {
        var amount = Clamp(request.Transaction.Amount / Normalization.Normalized[Normalization.MaxAmount]);
        var installments = Clamp(((float)request.Transaction.Installments) / Normalization.Normalized[Normalization.MaxInstallments]);
        var amountVsAvg = Clamp(request.Transaction.Amount / request.Customer.Avg_amount / Normalization.Normalized[Normalization.AmountVsAvgRatio]);
        var hourOfDay = request.Transaction.Requested_at.Hour / 23f;

        // necessary because .net's DateTime considers Sunday as the first day of the week, while in Brazil it's Monday
        var normalizedDayOfWeek = request.Transaction.Requested_at.AddDays(-1).DayOfWeek.GetHashCode();
        var dayOfWeek = normalizedDayOfWeek / 6f;

        var lastTran = request.Last_transaction;
        float minutesSinceLastTx = -1;
        float kmSinceLastTx = -1;
        if (lastTran != null)
        {
            var diff = (float)(lastTran.Timestamp - request.Transaction.Requested_at).TotalMinutes;
            minutesSinceLastTx = Clamp(diff / Normalization.Normalized[Normalization.MaxMinutes]);

            kmSinceLastTx = Clamp(lastTran.Km_from_current / Normalization.Normalized[Normalization.MaxKm]);
        }

        var kmFromHome = Clamp(request.Terminal.Km_from_home / Normalization.Normalized[Normalization.MaxKm]);
        var txCount24h = Clamp((float)request.Customer.Tx_count_24h / Normalization.Normalized[Normalization.MaxTxCount24h]);
        var isOnline = request.Terminal.Is_online ? 1f : 0f;
        var cardPresent = request.Terminal.Card_present ? 1f : 0f;
        var unknowMerchant = request.Customer.Known_merchants.Contains(request.Merchant.Id) ? 0f : 1f;

        var mccRisk = MccRisk.Risk.GetValueOrDefault(request.Merchant.Mcc, 0.5f);
        var merchantAvgAmount = Clamp(request.Merchant.Avg_amount / Normalization.Normalized[Normalization.MaxMerchantAvgAmount]);

        return [.. new[] {
                    amount,
                    installments,
                    amountVsAvg,
                    hourOfDay,
                    dayOfWeek,
                    minutesSinceLastTx,
                    kmSinceLastTx,
                    kmFromHome,
                    txCount24h,
                    isOnline,
                    cardPresent,
                    unknowMerchant,
                    mccRisk,
                    merchantAvgAmount
                }.Select(Round)];
    }
    private static float Clamp(float value)
    {
        return Math.Clamp(value, 0, 1);
    }
    private static float Round(float value)
    {
        return (float)Math.Round(value, 4);
    }
}
