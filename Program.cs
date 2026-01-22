using VoiceToText.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<IWhisperService, WhisperService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// API endpoint for Whisper transcription
app.MapPost("/api/transcribe", async (HttpRequest request, IWhisperService whisperService) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest("Expected multipart form data");
    }

    var form = await request.ReadFormAsync();
    var audioFile = form.Files.GetFile("audio");
    var language = form["language"].FirstOrDefault() ?? "en";

    if (audioFile == null || audioFile.Length == 0)
    {
        return Results.BadRequest("No audio file provided");
    }

    if (!whisperService.IsAvailable())
    {
        return Results.Problem(
            detail: "Whisper is not available on this server. Please use Browser API mode or install whisper.cpp.",
            statusCode: 503
        );
    }

    try
    {
        await using var stream = audioFile.OpenReadStream();
        var result = await whisperService.TranscribeAsync(stream, language);
        return Results.Ok(new {
            text = result.Text,
            stats = new {
                processingTimeMs = result.Stats.ProcessingTimeMs,
                audioFileSizeBytes = result.Stats.AudioFileSizeBytes,
                audioDurationSeconds = result.Stats.AudioDurationSeconds
            }
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// API endpoint to check Whisper availability
app.MapGet("/api/whisper/status", (IWhisperService whisperService) =>
{
    return Results.Ok(new { available = whisperService.IsAvailable() });
});

// API endpoint to get model info
app.MapGet("/api/whisper/model", (IWhisperService whisperService) =>
{
    var info = whisperService.GetModelInfo();
    return Results.Ok(new {
        modelName = info.ModelName,
        modelSizeBytes = info.ModelSizeBytes,
        modelSizeFormatted = info.ModelSizeFormatted,
        modelPath = info.ModelPath
    });
});

app.Run();
