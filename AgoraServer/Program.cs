using AgoraServer.Data;
using Microsoft.AspNetCore.ResponseCompression;
using AgoraServer.Hubs;

// TODO: make a landing page for recommendations and my Github

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();

// Configure socket stream
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Assign the hub to be used being our custom class
app.MapBlazorHub();
app.MapHub<UserHub>("/userhub");
app.MapFallbackToPage("/_Host");

app.Run();
