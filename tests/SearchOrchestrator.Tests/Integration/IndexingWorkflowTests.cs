using Microsoft.AspNetCore.Mvc.Testing;
using SearchOrchestrator.Application.DTOs.Requests;
using SearchOrchestrator.Application.DTOs.Responses;
using System.Net;
using System.Net.Http.Json;

namespace SearchOrchestrator.Tests.Integration;

public class IndexingWorkflowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IndexingWorkflowTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StartIndexing_Returns202WithLocation()
    {
        var request = new StartIndexingRequest("IntegrationSource", "/integration/path");

        var response = await _client.PostAsJsonAsync("/api/indexing/tasks", request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task GetTask_NonExistent_Returns404()
    {
        var response = await _client.GetAsync($"/api/indexing/tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FullWorkflow_CreateAndPollStatus()
    {
        var request = new StartIndexingRequest("WorkflowSource", "/workflow/path");

        var createResponse = await _client.PostAsJsonAsync("/api/indexing/tasks", request);
        var task = await createResponse.Content.ReadFromJsonAsync<IndexingTaskResponse>();
        Assert.NotNull(task);

        // Poll for completion (background processor should pick it up)
        IndexingTaskResponse? status = null;
        for (var i = 0; i < 20; i++)
        {
            await Task.Delay(500);
            var statusResponse = await _client.GetAsync($"/api/indexing/tasks/{task.TaskId}");
            status = await statusResponse.Content.ReadFromJsonAsync<IndexingTaskResponse>();
            if (status?.Status is "Completed" or "PartialSuccess" or "Failed")
                break;
        }

        Assert.NotNull(status);
        Assert.NotEqual("Pending", status.Status);
    }

    [Fact]
    public async Task Search_ReturnsOk()
    {
        var request = new SearchRequest("test-query");

        var response = await _client.PostAsJsonAsync("/api/search", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SearchResponse>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ListTasks_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/indexing/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CorrelationId_PropagatedInResponse()
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/indexing/tasks");
        request.Headers.Add("X-Correlation-Id", correlationId);

        var response = await _client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var values));
        Assert.Contains(correlationId, values);
    }
}
