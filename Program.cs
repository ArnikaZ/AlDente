using AlDentev2.Data;
using AlDentev2.Models;
using AlDentev2.Repositories;
using AlDentev2.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
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

            // Add Identity services
            builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.AddAuthentication()
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
            })
            .AddFacebook(options =>
            {
                options.AppId = builder.Configuration["Authentication:Facebook:AppId"];
                options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
            });

            //rejestracja repozytoriów
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<ICartRepository, CartRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IShippingMethodRepository, ShippingMethodRepository>();
            builder.Services.AddScoped<IEmailSender, GmailEmailSender>();

            //konfiguracja obs³ugi sesji dla utrzymania stanu koszyka niezalogowanych u¿ytkowników
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
                   //context.Database.EnsureDeleted();
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
            app.UseAuthorization();

            app.MapRazorPages();
            

            app.Run();
        }
    }
}
