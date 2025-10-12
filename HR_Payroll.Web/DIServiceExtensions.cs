using HR_Payroll.Core.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;

namespace HR_Payroll.Web
{
    public static class DIServiceExtensions
    {
        public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddMvc();
            builder.Services.AddSession();
            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpClient("AuthClient", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]);
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
                );
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                UseProxy = false, // Disable if not needed
                MaxConnectionsPerServer = 10
            }).SetHandlerLifetime(TimeSpan.FromMinutes(5)); // Connection pooling

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        .AddCookie(options =>
                        {
                            options.LoginPath = "/Home/Login";
                            options.LogoutPath = "/Home/Logout";
                            options.AccessDeniedPath = "/Home/AccessDenied";
                            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                            options.SlidingExpiration = true;
                        });
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/Home/AccessDenied";
            });
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                {
                    policy.RequireClaim(ApplicationClaims.RoleName, "Admin");
                });
            });
            //builder.Services.AddSingleton<LanguageService>();
            //builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
            //builder.Services.AddMvc()
            //    .AddViewLocalization()
            //    .AddDataAnnotationsLocalization(options =>
            //    {
            //        options.DataAnnotationLocalizerProvider = (type, factory) =>
            //        {
            //            var assemblyName = new AssemblyName(typeof(SharedResource).GetTypeInfo().Assembly.FullName);
            //            return factory.Create("SharedResource", assemblyName.Name);
            //        };
            //    });

            //builder.Services.Configure<RequestLocalizationOptions>(options =>
            //{
            //    var supportedCultures = new List<CultureInfo>
            //{
            //    new CultureInfo("en-US"),
            //    new CultureInfo("or-IN")
            //};

            //    options.DefaultRequestCulture = new RequestCulture(
            //        culture: builder.Configuration["DefaultCulture"],
            //        uiCulture: builder.Configuration["DefaultCulture"]);
            //    options.SupportedCultures = supportedCultures;
            //    options.SupportedUICultures = supportedCultures;
            //    options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
            //});
            //builder.Services.AddTransient<UnauthorizedRequestHandler>();
            return builder.Build();
        }
        public static WebApplication ConfigurePipeline(this WebApplication app)
        {
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            // app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000");
                }
            });
            // Configure localization
            app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
            app.UseSession();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Login}/{id?}");
            return app;
        }
    }
}
