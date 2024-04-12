using DevJournal.WebApp.Components;
using DevJournal.WebApp.Hubs;
using Microsoft.AspNetCore.ResponseCompression;
using Plus.AutoApi;
using _Imports = DevJournal.WebApp.Client._Imports;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

//builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAutoApi();

var app = builder.Build();

app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(_Imports).Assembly);
app.MapHub<ChatHub>("/chathub");

app.Run();