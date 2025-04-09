using AlDentev2.Data;
using AlDentev2.Repositories;
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

            //rejestracja repozytoriów
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<ICartRepository, CartRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IShippingMethodRepository, ShippingMethodRepository>();

            //konfiguracja obs³ugi sesji
            builder.Services.AddDistributedMemoryCache(); //do przechwywania danych w sesji
            builder.Services.AddSession(options => //w³¹cza obs³ugê sesji w aplikacji
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); //czas wygaœniêcia sesji
                options.Cookie.HttpOnly = true; //Zabezpiecza plik cookie sesji przed dostêpem po stronie klienta (skrypty JavaScript-zmiejsza ryzyko XSS)
                options.Cookie.IsEssential = true; // Oznacza plik cookie sesji jako niezbêdny
            });
            
            var app = builder.Build();

            //inicializacja bazy danych przy starcie aplikacji
            using(var scope = app.Services.CreateScope()) //tworzy nowy zakres us³ug (otwarcie nowej sesji)
            {
                var services = scope.ServiceProvider; //pobiera dostawcê uslug
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>(); //pobranie kontekstu bazy danych
                    context.Database.EnsureDeleted();
                    await DbInitializer.InitializeAsync(context); //tworzy bazê danych jeœli nie istnieje i dodaje pocz¹tkowe dane
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
            app.UseSession();

            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
    }
}
