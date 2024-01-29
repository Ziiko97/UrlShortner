
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using UrlShort.Models;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApiDbContext>(options => options.UseMySql(connection, ServerVersion.AutoDetect(connection), null));

//This is command class which will handle the query and connection object.


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//logic
app.MapPost("/shorturl", async (Dto url, ApiDbContext db, HttpContext ctx) =>
{
    if (!Uri.TryCreate(url.url, UriKind.Absolute, out var inputUrl))
        return Results.BadRequest("Invalid URL");

    //creating short version of URL
    var random = new Random();
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789@az";
    var randomChars = new string(Enumerable.Repeat(chars, 8)
        .Select(x => x[random.Next(x.Length)]).ToArray());


    //mapping short url w long one
    var sUrl = new UrlShortner()
    {
        url = url.url,
        shortUrl = randomChars
    };


    //Saving
    //string Query = $"insert into Urls(id,url,shortUrl) values({randomChars},{url},{sUrl})";

    db.Urls.Add(sUrl);
    await db.SaveChangesAsync();

    //construct url
    var result = $"{ctx.Request.Scheme}://{ctx.Request.Host}://{sUrl.shortUrl}";

    return Results.Ok(new ResponseDto()
    {
        url = result
    });
});

app.MapFallback(async (ApiDbContext db, HttpContext ctx) =>
{
    var path = ctx.Request.Path.ToUriComponent().Trim('/');
    var urlMatch = await db.Urls.FirstOrDefaultAsync(x =>
    x.shortUrl.Trim() == path.Trim());


    if (urlMatch == null)
        return Results.BadRequest("URL not found! Invalid short URL...");

    return Results.Redirect(urlMatch.url);
    

    
});

app.Run();

class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
    {

    }
    public virtual DbSet<UrlShortner> Urls
    {
        get;set;
    }
}
