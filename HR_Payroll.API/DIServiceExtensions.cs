using HR_Payroll.API.Config;
using HR_Payroll.API.JWTExtension;
using HR_Payroll.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace HR_Payroll.API
{
    public static class DIServiceExtensions
    {
        private const string CorsPolicy = "_MyAllowDomainPolicy";
        private const string JwtIdentitySetting = "JwtIdentitySetting";

        public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
        {
            // Configure DbContext with SQL Server and advanced options
            // Context 1: For EF Core operations(with tracking)
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    sqlServerOptions =>
                    {
                        sqlServerOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null
                        );
                        sqlServerOptions.CommandTimeout(30);
                    }
                );
                // ✅ Keep tracking enabled for EF Core operations
            });

            // configure authentication to use IdentityServer
            var identityServerSettings = builder.Configuration.GetSection("JwtIdentitySetting").Get<JwtIdentitySetting>();
            builder.Services.Configure<JwtIdentitySetting>(builder.Configuration.GetSection(JwtIdentitySetting));

            // add the configuration settings to the dependency injection container
            builder.Services.AddSingleton(identityServerSettings);

            builder.Services.AddHttpContextAccessor();

            //builder.Services.AddControllers().AddNewtonsoftJson(options =>{options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;});
            builder.Services.AddControllers().AddNewtonsoftJson();

            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            builder.Services.ConfigureDIServices();

            builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, builder => builder.WithOrigins(identityServerSettings.AllowedOrigins).AllowAnyHeader().AllowAnyMethod()));
            //builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
            
            // Add Authorization Policies
            // builder.Services.ConfigureAuthorizationPolicies();


            // Add Authentication
            var key = Encoding.UTF8.GetBytes(identityServerSettings.Secret);

            builder.Services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = identityServerSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = identityServerSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true, // Ensures token expiration is checked
                    ClockSkew = TimeSpan.Zero // Remove default 5-minute tolerance
                };

                // Optional: add events for debugging / refresh handling
                x.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json";

                            var response = new
                            {
                                status = false,
                                message = "Token expired. Please login again."
                            };

                            var json = System.Text.Json.JsonSerializer.Serialize(response);
                            return context.Response.WriteAsync(json);
                        }

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var invalid = new
                        {
                            status = false,
                            message = "Invalid token."
                        };

                        var invalidJson = System.Text.Json.JsonSerializer.Serialize(invalid);
                        return context.Response.WriteAsync(invalidJson);
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse(); // Prevents the default 401 redirect

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var response = new ErrorResponse
                        {
                            status = false,
                            message = "Unauthorized access"
                        };

                        return context.Response.WriteAsync(response.ToString());
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";

                        var response = new ErrorResponse
                        {
                            status = false,
                            message = "Forbidden"
                        };

                        return context.Response.WriteAsync(response.ToString());
                    }
                };
            });
     
            builder.Services.AddAuthorization();

            // Basic essential services first
            builder.Services.AddControllers().AddNewtonsoftJson();

            builder.Services.AddEndpointsApiExplorer();

            // builder.Services.AddSwaggerGen();
            RegisterDocumentationGenerators(builder.Services);

            return builder.Build();
        }

        public static WebApplication ConfigurePipeline(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseStaticFiles();
            app.UseCors(CorsPolicy);
            app.UseAuthentication();
            app.UseAuthorization();
            app.ConfigureRedundantStatusCodePages(); // Provide JSON responses for standard response codes such as HTTP 401.
            app.ConfigureExceptionHandler();
            //app.UseHttpContextHelper(); // Helper to get Base URL anywhere in application
            //InitializeRoles(app.Services).Wait();
            //InitializeUser(app.Services).Wait();
            app.UseEndpoints(endpoints =>
            {
                _=endpoints.MapDefaultControllerRoute();
            });

            // Add diagnostic middleware
            app.Use(async (context, next) =>
            {
                try
                {
                    Console.WriteLine($"Incoming request to: {context.Request.Path}");
                    await next();
                    Console.WriteLine($"Response status code: {context.Response.StatusCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing request: {ex.Message}");
                    throw;
                }
            });

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                _ = endpoints.MapControllers();
            });

            return app;
        }

        private static void RegisterDocumentationGenerators(IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "DRCTS_Payroll.API",
                    Description = "An ASP.NET Core Web API for managing DORMS.Api items"
                });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type=ReferenceType.SecurityScheme,
                                    Id="Bearer"
                                }
                            },
                        new string[]{}
                    }
                });

            });
        }

    }
}
