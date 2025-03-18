using AlDentev2.Data;
using Microsoft.EntityFrameworkCore;

namespace AlDentev2
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

            // Add services to the container.
            builder.Services.AddRazorPages();

            //dodanie kontekstu bazy danych
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            var app = builder.Build();

            //inicializacja bazy danych przy starcie aplikacji
            using(var scope = app.Services.CreateScope()) //tworzy nowy zakres us�ug (otwarcie nowej sesji)
            {
                var services = scope.ServiceProvider; //pobiera dostawc� uslug
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>(); //pobranie kontekstu bazy danych
                    await DbInitializer.InitializeAsync(context); //tworzy baz� danych je�li nie istnieje i dodaje pocz�tkowe dane
                }
                catch(Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Error in initializing database");
                }
            }

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

            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
    }
}
