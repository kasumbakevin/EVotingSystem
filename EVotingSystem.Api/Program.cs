using EVotingSystem.Data;
using EVotingSystem.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// The API shares the Core data layer. Voting, invitation and reporting endpoints
// are implemented in Stage 3; for now the host establishes the boundary and
// verifies database connectivity.
builder.Services.AddSingleton(_ => new DatabaseFactory(
    builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", (DatabaseFactory databaseFactory) =>
{
    using var context = databaseFactory.CreateContext();
    return Results.Ok(new
    {
        status = "ok",
        database = databaseFactory.DatabasePath,
        elections = context.Elections.Count(),
        candidates = context.Candidates.Count(),
    });
});

app.MapGet("/invite/{token}", (string token, DatabaseFactory databaseFactory, HttpRequest request) =>
{
    var invitationService = new InvitationService(
        databaseFactory,
        new CsvExportInvitationSender(Path.Combine(Path.GetTempPath(), "EVotingSystem", "invitations")),
        $"{request.Scheme}://{request.Host}");

    var voter = invitationService.ResolveInvitation(token);
    if (voter == null)
    {
        return Results.Text(
            "<!doctype html><html><body style=\"font-family:Segoe UI,sans-serif;padding:40px\">" +
            "<h2>Invitation link invalid or expired</h2>" +
            "<p>This invitation link is no longer valid. Please contact your election administrator for a new invitation.</p>" +
            "</body></html>",
            "text/html");
    }

    return Results.Text(
        "<!doctype html><html><body style=\"font-family:Segoe UI,sans-serif;padding:40px\">" +
        "<h2>Voting invitation</h2>" +
        $"<p>Welcome, <strong>{System.Net.WebUtility.HtmlEncode(voter.Name)}</strong>.</p>" +
        $"<p>Voter number: {System.Net.WebUtility.HtmlEncode(voter.VoterNumber)}</p>" +
        $"<p>Verification status: {voter.VerificationStatus}</p>" +
        "<p>Please continue in the EVoting desktop application to verify your identity and cast your vote.</p>" +
        "</body></html>",
        "text/html");
});

app.Run();
