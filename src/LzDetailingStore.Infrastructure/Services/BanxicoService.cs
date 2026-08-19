using System.Net.Http.Json;
using System.Text.Json;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.SharedKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LzDetailingStore.Infrastructure.Services;

public sealed class BanxicoService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<BanxicoService> logger) : IBanxicoService
{
    private const string Serie = "SF43718"; // USD/MXN fix

    public async Task<Result<decimal>> GetCurrentRateAsync(CancellationToken ct = default)
    {
        var token = configuration["Banxico:BmxToken"];

        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("Banxico BmxToken not configured");
            return Result<decimal>.Failure(
                new Error("BANXICO_CONFIG", "Token de Banxico no configurado."));
        }

        try
        {
            var client = httpClientFactory.CreateClient("Banxico");
            client.DefaultRequestHeaders.Add("Bmx-Token", token);

            var url = $"https://www.banxico.org.mx/SieAPIRest/service/v1/series/{Serie}/datos/oportuno";

            var response = await client.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Banxico API returned {StatusCode}", response.StatusCode);
                return Result<decimal>.Failure(
                    new Error("BANXICO_ERROR", "Error al consultar Banxico."));
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

            // Navigate: bmx.series[0].datos[0].dato
            var dato = json
                .GetProperty("bmx")
                .GetProperty("series")[0]
                .GetProperty("datos")[0]
                .GetProperty("dato")
                .GetString();

            if (dato is null || !decimal.TryParse(dato, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var rate))
            {
                logger.LogError("Banxico returned unparseable rate: {Dato}", dato);
                return Result<decimal>.Failure(
                    new Error("BANXICO_PARSE", "No se pudo parsear el tipo de cambio."));
            }

            logger.LogInformation("Banxico USD/MXN rate fetched: {Rate}", rate);
            return Result<decimal>.Success(rate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Banxico API exception");
            return Result<decimal>.Failure(
                new Error("BANXICO_ERROR", "Error de comunicación con Banxico."));
        }
    }
}
