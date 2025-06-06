using AlDentev2.Data;
using AlDentev2.Models;
using AlDentev2.Repositories;
using AlDentev2.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AlDentev2
{
    public class Program
    {
        
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddUserSecrets<Program>();

            // Add services to the container.
            builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

            // Dodanie kontekstu bazy danych
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.Configure<PasswordHasherOptions>(options =>
            {
                options.IterationCount = 100000;
            });

            // Add Identity services
            builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            
            builder.Services.AddAuthentication()
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
            })
            .AddFacebook(options =>
            {
                options.AppId = builder.Configuration["Authentication:Facebook:AppId"]!;
                options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"]!;
            });


            // Rejestracja repozytoriów
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<ICartRepository, CartRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IShippingMethodRepository, ShippingMethodRepository>();
            builder.Services.AddScoped<IEmailSender, GmailEmailSender>();

            // Konfiguracja sesji
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
               options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            var app = builder.Build();

            app.Use(async (context, next) =>
            {
                var csp = "default-src 'self'; " +
                          "script-src 'self' https://cdn.jsdelivr.net https://www.google.com/recaptcha/ https://www.gstatic.com/recaptcha/ ; " +
                          "style-src 'self' https://cdn.jsdelivr.net https://fonts.googleapis.com 'unsafe-hashes' 'sha256-aqNNdDLnnrDOnTNdkJpYlAxKVJtLt9CtFLklmInuUAE=' 'sha256-NHarn8wEqJqUQoKwsaJttWeSqzOSSPTy65p3Z6aS0Qs=' 'sha256-FJ70q/rOyGtVjs760UXock5Sd7Sb58E7hNt++vGP+5w=' 'sha256-6kmyWkdMnSnCJhCug/q/GhxflQ/fFQvTph5flwFCXlc=' 'sha256-8QkhJYL9zovWm/J9JEob9eStRUEcSD0wW1R3KJkTI7c=' 'sha256-JHifDLeo1FrY9lSi8/sIR91pHkTa67ozQWhP0zwLtlo=' ;  " +
                          "font-src https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
                          "img-src 'self' data:; " +
                          "media-src 'self'; " +
                          "connect-src 'self' https://www.google.com https://accounts.google.com https://www.facebook.com; " +
                          "form-action 'self' https://accounts.google.com https://www.facebook.com; " +
                          "frame-src 'self' https://www.google.com/recaptcha/; " +
                          "object-src 'none'; " +
                          "base-uri 'self'; " ;

                // Dodaj Ÿród³a dla BrowserLink i Browser Refresh w œrodowisku deweloperskim
                if (app.Environment.IsDevelopment())
                {
                    csp = csp.Replace(
                        "connect-src 'self' https://www.google.com https://accounts.google.com https://www.facebook.com",
                        "connect-src 'self' https://www.google.com https://accounts.google.com https://www.facebook.com http://localhost:* ws://localhost:* wss://localhost:*"
                    );
                }

                context.Response.Headers.Append("Content-Security-Policy", csp);
                await next();
            });

           
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var factory = new MigrationDbContextFactory();
                    using var context = factory.CreateDbContext(null);
                    await context.Database.MigrateAsync();
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Error during database migration");
                    throw; // Opcjonalnie: przerwij uruchamianie aplikacji lub kontynuuj w zale¿noœci od potrzeb
                }
            }
            if (app.Environment.IsDevelopment())
            {
                using (var scope = app.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await DbInitializer.InitializeAsync(context);
                }
            }
            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseRouting();



            app.UseAuthentication();
            app.UseAuthorization();
            app.MapRazorPages();

            app.Run();

        }
    }
}
