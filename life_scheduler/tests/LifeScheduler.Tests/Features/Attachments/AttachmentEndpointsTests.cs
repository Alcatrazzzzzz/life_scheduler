using System.Net;
using System.Net.Http.Json;
using LifeScheduler.Api.Features.Attachments;
using LifeScheduler.Api.Features.Schedule;
using LifeScheduler.Tests.Support;
using Microsoft.AspNetCore.Mvc;

namespace LifeScheduler.Tests.Features.Attachments;

public sealed class AttachmentEndpointsTests
{
    [Fact]
    public async Task AddAttachment_ThenUploadAndConfirm_MakesFileDownloadable()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();
        var scheduleId = await ScheduleTestHelpers.CreateEntryAsync(client);

        var content = "some pdf bytes"u8.ToArray();
        var attachmentId = await ScheduleTestHelpers.UploadAndConfirmAsync(
            client, scheduleId, "смета.pdf", "application/pdf", content);

        var detail = await client.GetFromJsonAsync<GetSchedule.Response>(
            $"/api/schedules/{scheduleId}", ScheduleTestHelpers.JsonOptions);

        Assert.NotNull(detail);
        var attachment = Assert.Single(detail.Attachments);
        Assert.Equal(attachmentId, attachment.Id);
        Assert.Equal("смета.pdf", attachment.FileName);
        Assert.Equal("application/pdf", attachment.ContentType);
        Assert.Equal(content.Length, attachment.SizeBytes);

        var list = await client.GetFromJsonAsync<List<ListSchedule.Response>>(
            "/api/schedules?from=2026-11-20&to=2026-11-20", ScheduleTestHelpers.JsonOptions);
        Assert.NotNull(list);
        Assert.Equal(1, Assert.Single(list).AttachmentCount);

        var noRedirect = ScheduleTestHelpers.CreateClientWithoutRedirect(factory);
        var download = await noRedirect.GetAsync(
            $"/api/schedules/{scheduleId}/attachments/{attachmentId}/download");

        Assert.Equal(HttpStatusCode.Redirect, download.StatusCode);
        Assert.NotNull(download.Headers.Location);
        Assert.Contains("attachment", download.Content.Headers.ContentDisposition?.DispositionType ?? "attachment");

        var downloaded = await noRedirect.GetByteArrayAsync(download.Headers.Location);
        Assert.Equal(content, downloaded);
        Assert.Single(ScheduleTestHelpers.StoredFiles(factory.StorageRoot));
    }

    [Fact]
    public async Task AddAttachment_SavesPendingRowWithoutShowingItInDetails()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();
        var scheduleId = await ScheduleTestHelpers.CreateEntryAsync(client);

        var ticket = await ScheduleTestHelpers.CreateTicketAsync(
            client, scheduleId, "фото.jpg", "image/jpeg", 10);

        Assert.Equal("PUT", ticket.Upload.Method);
        Assert.Equal("image/jpeg", ticket.Upload.Headers["Content-Type"]);
        Assert.Empty(ScheduleTestHelpers.StoredFiles(factory.StorageRoot));

        var detail = await client.GetFromJsonAsync<GetSchedule.Response>(
            $"/api/schedules/{scheduleId}", ScheduleTestHelpers.JsonOptions);
        Assert.NotNull(detail);
        Assert.Empty(detail.Attachments);
    }

    [Fact]
    public async Task ConfirmAttachment_WhenActualSizeDiffers_ReturnsUnprocessableAndRemovesFile()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();
        var scheduleId = await ScheduleTestHelpers.CreateEntryAsync(client);

        var ticket = await ScheduleTestHelpers.CreateTicketAsync(
            client, scheduleId, "док.txt", "text/plain", sizeBytes: 4);

        await ScheduleTestHelpers.UploadAsync(
            client, ticket.Upload.Url, "пять!"u8.ToArray(), "text/plain");

        var confirm = await client.PostAsync(
            $"/api/schedules/{scheduleId}/attachments/{ticket.Id}/confirm", content: null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, confirm.StatusCode);
        Assert.Empty(ScheduleTestHelpers.StoredFiles(factory.StorageRoot));

        var detail = await client.GetFromJsonAsync<GetSchedule.Response>(
            $"/api/schedules/{scheduleId}", ScheduleTestHelpers.JsonOptions);
        Assert.NotNull(detail);
        Assert.Empty(detail.Attachments);
    }

    [Fact]
    public async Task ConfirmAttachment_WhenObjectMissing_ReturnsNotFound()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();
        var scheduleId = await ScheduleTestHelpers.CreateEntryAsync(client);

        var ticket = await ScheduleTestHelpers.CreateTicketAsync(
            client, scheduleId, "док.txt", "text/plain", sizeBytes: 4);

        var confirm = await client.PostAsync(
            $"/api/schedules/{scheduleId}/attachments/{ticket.Id}/confirm", content: null);

        Assert.Equal(HttpStatusCode.NotFound, confirm.StatusCode);
    }

    [Fact]
    public async Task AddAttachment_WhenSingleFileTooLarge_ReturnsValidationProblem()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();
        var scheduleId = await ScheduleTestHelpers.CreateEntryAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/schedules/{scheduleId}/attachments",
            new AddAttachmentRequest("big.bin", "application/octet-stream", AttachmentLimits.MaxFileBytes + 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
            ScheduleTestHelpers.JsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey(nameof(AddAttachmentRequest.SizeBytes)));
    }

    [Fact]
    public async Task AddAttachment_WhenFileCountExceeded_ReturnsValidationProblem()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();
        var scheduleId = await ScheduleTestHelpers.CreateEntryAsync(client);

        for (var index = 0; index < AttachmentLimits.MaxFilesPerSchedule; index++)
        {
            await ScheduleTestHelpers.CreateTicketAsync(
                client, scheduleId, $"file-{index}.txt", "text/plain", 1);
        }

        var response = await client.PostAsJsonAsync(
            $"/api/schedules/{scheduleId}/attachments",
            new AddAttachmentRequest("extra.txt", "text/plain", 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
            ScheduleTestHelpers.JsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey(nameof(AddAttachmentRequest.FileName)));
    }

    [Fact]
    public async Task AddAttachment_WhenTotalSizeExceeded_ReturnsValidationProblem()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();
        var scheduleId = await ScheduleTestHelpers.CreateEntryAsync(client);

        for (var index = 0; index < 4; index++)
        {
            await ScheduleTestHelpers.CreateTicketAsync(
                client, scheduleId, $"file-{index}.bin", "application/octet-stream",
                AttachmentLimits.MaxFileBytes);
        }

        var response = await client.PostAsJsonAsync(
            $"/api/schedules/{scheduleId}/attachments",
            new AddAttachmentRequest("extra.bin", "application/octet-stream", 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
            ScheduleTestHelpers.JsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey(nameof(AddAttachmentRequest.SizeBytes)));
    }

    [Fact]
    public async Task DeleteAttachment_RemovesMetadataAndObject()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();
        var scheduleId = await ScheduleTestHelpers.CreateEntryAsync(client);

        var attachmentId = await ScheduleTestHelpers.UploadAndConfirmAsync(
            client, scheduleId, "док.txt", "text/plain", "text"u8.ToArray());

        await client.DeleteAsync($"/api/schedules/{scheduleId}/attachments/{attachmentId}");

        Assert.Empty(ScheduleTestHelpers.StoredFiles(factory.StorageRoot));

        var detail = await client.GetFromJsonAsync<GetSchedule.Response>(
            $"/api/schedules/{scheduleId}", ScheduleTestHelpers.JsonOptions);
        Assert.NotNull(detail);
        Assert.Empty(detail.Attachments);
    }

    [Fact]
    public async Task DeleteSchedule_RemovesAttachmentObjects()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();
        var scheduleId = await ScheduleTestHelpers.CreateEntryAsync(client);

        await ScheduleTestHelpers.UploadAndConfirmAsync(
            client, scheduleId, "док.txt", "text/plain", "text"u8.ToArray());

        await client.DeleteAsync($"/api/schedules/{scheduleId}");

        Assert.Empty(ScheduleTestHelpers.StoredFiles(factory.StorageRoot));
    }
}
