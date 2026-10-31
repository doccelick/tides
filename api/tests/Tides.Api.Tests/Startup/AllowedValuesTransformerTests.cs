using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tides.Api.Tests.Startup;

public class AllowedValuesTransformerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task OpenApi_ParameterWithAllowedValues_ListsValuesAsEnum()
    {
        var interval = await GetSamplesParameterAsync("interval");

        var values = interval["schema"]!["enum"]!.AsArray().Select(v => v!.GetValue<int>());

        Assert.Equal(new[] { 10, 60 }, values);
    }

    [Fact]
    public async Task OpenApi_ParameterWithoutAllowedValues_HasNoEnum()
    {
        var days = await GetSamplesParameterAsync("days");

        Assert.Null(days["schema"]!["enum"]);
    }

    private async Task<JsonNode> GetSamplesParameterAsync(string name)
    {
        var client = factory.CreateClient();
        var json = await client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        var parameters = JsonNode.Parse(json)!["paths"]!["/v1/locations/{id}/samples"]!["get"]!["parameters"]!.AsArray();

        return parameters.Single(p => p!["name"]!.GetValue<string>() == name)!;
    }
}
