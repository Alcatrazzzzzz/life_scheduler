using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LifeScheduler.Api.Features.Attachments;
using LifeScheduler.Api.Features.Schedule;
using LifeScheduler.Api.Shared.Data;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LifeScheduler.Tests.Support;

internal static class ScheduleTestHelpers
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static async Task<Guid> CreateEntryAsync(
        HttpClient client,
        string title = "Смета",
        DateOnly? date = null,
        TimeOnly? start = null,
        TimeOnly? end = null,
        ScheduleRecurrence recurrence = ScheduleRecurrence.None)
    {
        var request = new CreateScheduleRequest(
            title,
            date ?? new DateOnly(2026, 11, 20),
            start,
            end,
            null,
            recurrence);

        var response = await client.PostAsJsonAsync("/api/schedules", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<CreateSchedule.Response>(JsonOptions);
        Assert.NotNull(created);
        return created.Id;
    }

    public static async Task<AddAttachment.Response> CreateTicketAsync(
        HttpClient client,
        Guid scheduleId,
        string fileName,
        string contentType,
        long sizeBytes)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/schedules/{scheduleId}/attachments",
            new AddAttachmentRequest(fileName, contentType, sizeBytes));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);

        var created = JsonSerializer.Deserialize<AddAttachment.Response>(body, JsonOptions);
        Assert.NotNull(created);
        return created;
    }

    public static async Task UploadAsync(
        HttpClient client,
        string uploadUrl,
        byte[] content,
        string contentType)
    {
        using var body = new ByteArrayContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var response = await client.PutAsync(uploadUrl, body);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public static async Task<Guid> UploadAndConfirmAsync(
        HttpClient client,
        Guid scheduleId,
        string fileName,
        string contentType,
        byte[] content)
    {
        var ticket = await CreateTicketAsync(client, scheduleId, fileName, contentType, content.Length);
        await UploadAsync(client, ticket.Upload.Url, content, contentType);

        var confirm = await client.PostAsync(
            $"/api/schedules/{scheduleId}/attachments/{ticket.Id}/confirm",
            content: null);

        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
        return ticket.Id;
    }

    public static HttpClient CreateClientWithoutRedirect(ScheduleApiFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public static string[] StoredFiles(string storageRoot) =>
        Directory.Exists(storageRoot)
            ? Directory.GetFiles(storageRoot, "*", SearchOption.AllDirectories)
            : [];
}
