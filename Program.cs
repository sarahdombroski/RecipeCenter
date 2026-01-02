using TeamProjectYay.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TeamProjectYay.Data;
using TeamProjectYay.Services; // this is for IEmailService and EmailService

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthenticationCore();
builder.Services.AddAuthorizationCore();
// builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<FileAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<FileAuthStateProvider>());

// Configure FluentEmail
builder.Services
    .AddFluentEmail(builder.Configuration["Email:FromAddress"])
    .AddSmtpSender(
        builder.Configuration["Email:SmtpServer"], // from appsettings.json.
        int.Parse(builder.Configuration["Email:SmtpPort"] ?? "587"), // this port is set by the email provider. 587 is standard for TLS
        builder.Configuration["Email:Username"], // from appsettings.json.
        builder.Configuration["Email:Password"]  // from appsettings.json.
    );

builder.Services.AddScoped<IEmailService, EmailService>(); // Register EmailService for IEmailService


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
