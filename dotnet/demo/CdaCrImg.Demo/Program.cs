// Application web de démonstration de la librairie CdaCrImg : un formulaire décrivant chaque champ
// géré par la librairie, qui renvoie le CDA XML produit.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.Run();

/// <summary>Point d'entrée, exposé pour les tests d'intégration (WebApplicationFactory).</summary>
public partial class Program
{
}
