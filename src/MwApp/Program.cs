using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using MwApp;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL, from the binding: ConnectionStrings__DefaultConnection, in
// Npgsql's format, through the pooler (the release step gets the server).
builder.Services.AddDbContext<AppDb>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Login cookies and form tokens are encrypted with keys every instance has
// to share, across restarts and deploys: in the database, not on the
// container's disk, which each instance has its own of and loses.
builder.Services.AddDataProtection().PersistKeysToDbContext<AppDb>().SetApplicationName("mw-app");
builder.Services.AddAntiforgery();
builder.Services.AddSingleton<Storage>();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);

var app = builder.Build();
app.UseStaticFiles();
app.UseAntiforgery();

// The platform's health check: the process answers.
app.MapGet("/health", () => Results.Json(new { ok = true }));

app.MapGet("/status.json", async (AppDb db, Storage storage, CancellationToken ct) =>
{
    var checks = await Checks(db, storage, ct);
    var ok = checks.Values.All(c => c.Ok);
    return Results.Json(new { ok, checks }, statusCode: ok ? 200 : 503);
});

app.MapGet("/api/files", async (AppDb db, Storage storage, ILogger<Program> log, CancellationToken ct) =>
{
    try { return Results.Json(new { files = await Files(db, storage, ct) }); }
    catch (Exception e)
    {
        log.LogError(e, "listing failed");
        return Results.Json(new { error = "The database or the bucket is not reachable: see /status.json." }, statusCode: 503);
    }
});

// For API clients: no browser session, so no form token to check.
app.MapPost("/api/files", async (IFormFile? file, AppDb db, Storage storage, CancellationToken ct) =>
{
    if (file is null || file.Length == 0) return Results.Json(new { error = "Send the file in the \"file\" field." }, statusCode: 400);
    var row = await Save(file, db, storage, ct);
    return Results.Json(new { id = row.Id, name = row.Name, url = await storage.SignedLink(row.ObjectKey, row.Name) }, statusCode: 201);
}).DisableAntiforgery();

// The page's form: its token is checked (UseAntiforgery), whichever
// instance made it — the keys are the database's.
app.MapPost("/files", async (IFormFile? file, AppDb db, Storage storage, ILogger<Program> log, CancellationToken ct) =>
{
    if (file is null || file.Length == 0) return Results.Redirect("/?error=empty");
    try
    {
        var row = await Save(file, db, storage, ct);
        return Results.Redirect($"/?sent={Uri.EscapeDataString(row.Name)}");
    }
    catch (Exception e)
    {
        log.LogError(e, "upload failed");
        return Results.Redirect("/?error=failed");
    }
});

app.MapGet("/", async (HttpContext http, IAntiforgery antiforgery, AppDb db, Storage storage, ILogger<Program> log, CancellationToken ct) =>
{
    var t = Texts.For(http.Request.Headers.AcceptLanguage);
    var checks = await Checks(db, storage, ct);
    var files = checks.Values.All(c => c.Ok) ? await Files(db, storage, ct) : new List<FileView>();
    // The form's token is sealed with the keys kept in the database: without
    // it the page still renders, and says what to do.
    AntiforgeryTokenSet? token = null;
    if (checks["database"].Ok)
        try { token = antiforgery.GetAndStoreTokens(http); }
        catch (Exception e) { log.LogError(e, "form token"); }
    var sent = http.Request.Query["sent"].ToString();
    var error = http.Request.Query["error"].ToString();
    string E(string s) => WebUtility.HtmlEncode(s);
    var labels = new Dictionary<string, string> { ["database"] = "PostgreSQL", ["storage"] = "Object Storage" };
    var note = sent != "" ? $"""<p class="note ok" role="status">{E(t.Sent(sent))}</p>"""
        : error != "" ? $"""<p class="note bad" role="alert">{E(error == "empty" ? t.Empty : t.Failed)}</p>""" : "";
    var checkList = string.Join("\n", checks.Select(c =>
        $"""<li data-check="{c.Key}" data-ok="{(c.Value.Ok ? "true" : "false")}"><span class="dot {(c.Value.Ok ? "ok" : "bad")}"></span><span class="name">{labels[c.Key]}</span><span class="detail {(c.Value.Ok ? "" : "bad")}">{E(c.Value.Detail)}</span></li>"""));
    var noDatabase = checks["database"].Ok ? "" : $"""<p class="note bad" role="alert" data-testid="no-database">{E(t.NoDatabase)}</p>""";
    var form = token is null
        ? $"""<p class="muted">{E(t.FormUnavailable)}</p>"""
        : $"""<form method="post" action="/files" enctype="multipart/form-data"><input type="hidden" name="{E(token.FormFieldName)}" value="{E(token.RequestToken ?? "")}"><input type="file" name="file" required aria-label="{E(t.File)}"><button type="submit">{E(t.Submit)}</button></form>""";
    var fileList = files.Count > 0
        ? $"""<ul class="files" data-testid="uploads">{string.Concat(files.Select(f => $"""<li><a href="{E(f.Url)}">{E(f.Name)}</a><span>{Size(f.Size)} · {E(t.Download)}</span></li>"""))}</ul>"""
        : $"""<p class="muted">{E(t.None)}</p>""";
    return Results.Content($"""
        <!DOCTYPE html>
        <html lang="{t.Lang}"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
        <title>ASP.NET Core · Master da Web</title><link rel="stylesheet" href="/style.css"></head>
        <body><main>
        <div class="brand">ASP.NET Core · Entity Framework Core · Master da Web</div>
        <h1>{E(t.Title)}</h1>
        <p class="lead">{E(t.Lead)}</p>
        <section><h2>{E(t.Reaches)}</h2><ul class="checks" data-testid="checks">
        {checkList}
        </ul>
        {noDatabase}</section>
        <section><h2>{E(t.Send)}</h2>
        {form}
        {note}
        {fileList}
        <p class="muted"><a href="/api/files">GET /api/files</a> — {E(t.Api)}</p>
        </section>
        <section><h2>{E(t.Next)}</h2><ol><li>{E(t.Step1)}</li><li>{E(t.Step2)}</li><li>{E(t.Step3)}</li></ol></section>
        <footer>.NET {Environment.Version} · <a href="/status.json">status.json</a></footer>
        </main></body></html>
        """, "text/html; charset=utf-8");
});

app.Run();

// Each part of the project, and whether this service reaches it.
static async Task<Dictionary<string, CheckResult>> Checks(AppDb db, Storage storage, CancellationToken ct)
{
    static async Task<CheckResult> Probe(Func<Task<string>> fn)
    {
        try { return new(true, await fn()); }
        catch (Exception e) { return new(false, e.Message.Split('\n')[0][..Math.Min(160, e.Message.Split('\n')[0].Length)]); }
    }
    return new()
    {
        ["database"] = await Probe(async () =>
            string.Join(' ', (await db.Database.SqlQuery<string>($"SELECT version() AS \"Value\"").SingleAsync(ct)).Split(' ').Take(2))),
        ["storage"] = await Probe(() => storage.Check(ct)),
    };
}

static async Task<List<FileView>> Files(AppDb db, Storage storage, CancellationToken ct)
{
    var list = new List<FileView>();
    foreach (var f in await db.Files.OrderByDescending(f => f.Id).Take(20).ToListAsync(ct))
        list.Add(new(f.Id, f.Name, f.Size, f.ContentType, f.CreatedAt, await storage.SignedLink(f.ObjectKey, f.Name)));
    return list;
}

// A file to the bucket, its row to PostgreSQL.
static async Task<StoredFile> Save(IFormFile file, AppDb db, Storage storage, CancellationToken ct)
{
    var key = $"files/{Guid.NewGuid()}";
    var type = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType;
    await using (var body = file.OpenReadStream()) await storage.Put(key, body, type, ct);
    var row = new StoredFile { ObjectKey = key, Name = Path.GetFileName(file.FileName), Size = file.Length, ContentType = type };
    db.Files.Add(row);
    await db.SaveChangesAsync(ct);
    return row;
}

static string Size(long n) => n >= 1048576 ? $"{n / 1048576.0:0.0} MB" : $"{Math.Max(1, (int)Math.Round(n / 1024.0))} KB";

record CheckResult(bool Ok, string Detail);

record FileView(long Id, string Name, long Size, string ContentType, DateTimeOffset CreatedAt, string Url);
