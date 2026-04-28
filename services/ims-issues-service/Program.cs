using IMS.Issues.Service.Infrastructure;
using IMS.Issues.Service.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<IssuesServiceDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Auth (same JWT as monolith) ───────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!))
        };
    });

builder.Services.AddAuthorization();

// ── RabbitMQ integration event consumer ──────────────────────────────────────
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddHostedService<IssuesEventConsumer>();
builder.Services.AddSingleton<IssuesEventPublisher>();

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<IssuesServiceDbContext>("issues-db");

// ── OpenAPI ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ── Health ────────────────────────────────────────────────────────────────────
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

// ── Issues Endpoints ──────────────────────────────────────────────────────────
var issues = app.MapGroup("/api/issues")
    .WithTags("Issues")
    .RequireAuthorization();

// GET /api/issues
issues.MapGet("/", async (
    IssuesServiceDbContext db,
    int pageNumber = 1, int pageSize = 20,
    string? status = null, string? priority = null, string? search = null,
    CancellationToken ct = default) =>
{
    var query = db.Issues.AsQueryable();

    if (!string.IsNullOrWhiteSpace(status))
        query = query.Where(i => i.Status == status);

    if (!string.IsNullOrWhiteSpace(priority))
        query = query.Where(i => i.Priority == priority);

    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(i => EF.Functions.ILike(i.Title, $"%{search}%")
                               || (i.Description != null && EF.Functions.ILike(i.Description, $"%{search}%")));

    var total = await query.CountAsync(ct);
    var items = await query
        .OrderByDescending(i => i.CreatedAt)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    return Results.Ok(new { total, pageNumber, pageSize, items });
});

// GET /api/issues/{id}
issues.MapGet("/{id:guid}", async (Guid id, IssuesServiceDbContext db, CancellationToken ct) =>
{
    var issue = await db.Issues.FindAsync([id], ct);
    return issue is null ? Results.NotFound() : Results.Ok(issue);
});

// POST /api/issues
issues.MapPost("/", async (CreateIssueRequest req, IssuesServiceDbContext db, IssuesEventPublisher publisher, CancellationToken ct) =>
{
    var issue = new IssueRecord
    {
        Id = Guid.NewGuid(),
        Title = req.Title,
        Description = req.Description,
        Status = "Open",
        Priority = req.Priority ?? "Medium",
        ReporterId = req.ReporterId,
        DueDate = req.DueDate,
        CreatedAt = DateTime.UtcNow
    };

    db.Issues.Add(issue);
    await db.SaveChangesAsync(ct);

    await publisher.PublishAsync("issues.created", new
    {
        issue.Id,
        issue.Title,
        issue.Priority,
        issue.ReporterId,
        issue.CreatedAt
    });

    return Results.Created($"/api/issues/{issue.Id}", issue);
});

// PUT /api/issues/{id}
issues.MapPut("/{id:guid}", async (Guid id, UpdateIssueRequest req, IssuesServiceDbContext db, CancellationToken ct) =>
{
    var issue = await db.Issues.FindAsync([id], ct);
    if (issue is null) return Results.NotFound();

    if (req.Title is not null) issue.Title = req.Title;
    if (req.Description is not null) issue.Description = req.Description;
    if (req.Priority is not null) issue.Priority = req.Priority;
    if (req.DueDate.HasValue) issue.DueDate = req.DueDate;
    issue.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(ct);
    return Results.Ok(issue);
});

// PATCH /api/issues/{id}/status
issues.MapPatch("/{id:guid}/status", async (Guid id, ChangeStatusRequest req, IssuesServiceDbContext db, IssuesEventPublisher publisher, CancellationToken ct) =>
{
    var issue = await db.Issues.FindAsync([id], ct);
    if (issue is null) return Results.NotFound();

    var oldStatus = issue.Status;
    issue.Status = req.Status;
    issue.UpdatedAt = DateTime.UtcNow;

    if (req.Status is "Closed" or "Resolved")
        issue.ResolvedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(ct);

    await publisher.PublishAsync("issues.status_changed", new
    {
        issue.Id,
        OldStatus = oldStatus,
        NewStatus = req.Status,
        ChangedAt = DateTime.UtcNow
    });

    return Results.Ok(issue);
});

// PATCH /api/issues/{id}/assign
issues.MapPatch("/{id:guid}/assign", async (Guid id, AssignRequest req, IssuesServiceDbContext db, CancellationToken ct) =>
{
    var issue = await db.Issues.FindAsync([id], ct);
    if (issue is null) return Results.NotFound();

    issue.AssigneeId = req.AssigneeId;
    issue.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(ct);
    return Results.Ok(issue);
});

// DELETE /api/issues/{id}
issues.MapDelete("/{id:guid}", async (Guid id, IssuesServiceDbContext db, IssuesEventPublisher publisher, CancellationToken ct) =>
{
    var issue = await db.Issues.FindAsync([id], ct);
    if (issue is null) return Results.NotFound();

    db.Issues.Remove(issue);
    await db.SaveChangesAsync(ct);

    await publisher.PublishAsync("issues.deleted", new { id, DeletedAt = DateTime.UtcNow });

    return Results.NoContent();
});

// ── Run ───────────────────────────────────────────────────────────────────────
app.Run();

// ── Request/Response Records ──────────────────────────────────────────────────
record CreateIssueRequest(string Title, string? Description, string? Priority, Guid ReporterId, DateTime? DueDate);
record UpdateIssueRequest(string? Title, string? Description, string? Priority, DateTime? DueDate);
record ChangeStatusRequest(string Status);
record AssignRequest(Guid? AssigneeId);
