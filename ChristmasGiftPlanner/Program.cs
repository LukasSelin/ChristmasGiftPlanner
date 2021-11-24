using ChristmasGiftPlanner;
using ChristmasGiftPlanner.Models;
using ChristmasGiftPlanner.Service;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
WhishlistService.createClient(true);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("localhost:7251/") });
builder.Services.AddScoped(sp => new WhishlistService());

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

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

