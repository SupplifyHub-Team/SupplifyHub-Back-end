using API.Filters;
using API.Services;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DAL;
using DAL.Data;
using dotenv.net;
using Entities;
using Hangfire;
using Hangfire.PostgreSql;
using Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using YourNamespace;
using static System.Net.WebRequestMethods;

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            // error handling
            builder.Services.Configure<ApiBehaviorOptions>(options => {
                options.SuppressModelStateInvalidFilter = true;
            });
            // Database Configuration
            builder.Services.AddDbContext<AppDBContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("con")));
            //email service
            builder.Services.AddScoped<IEmailService, EmailService>();
            //token service
            builder.Services.AddScoped<TokenService>();
            // memorycash
            builder.Services.AddMemoryCache();

            // Application Services
            builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.Configure<CloudinaryOptions>(
            builder.Configuration.GetSection("Cloudinary")
            );
            builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
            // Authentication Configuration
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = builder.Configuration["JWT:Issuer"],
                    ValidAudience = builder.Configuration["JWT:Audience"],
                    ValidateLifetime = true,
                    ValidateTokenReplay = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]!))
                };
                // FIXED: Correct OPTIONS request handling
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Method == "OPTIONS")
                        {
                            // Skip authentication for OPTIONS requests
                            context.NoResult(); // This is the correct approach
                            return Task.CompletedTask;
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // Swagger Configuration
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "B2BPlatform", Version = "v1" });
                c.DocumentFilter<EnumDocumentFilter>();
                c.CustomSchemaIds(type => type.FullName);

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] [token]",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // CORS Configuration
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("MyPolicy", policy =>
                {
                    //AllowAnyOrigin()
                    policy.WithOrigins("http://localhost:5173",
                    "http://localhost:5174",
                    "http://localhost:5175",
                    "http://localhost:4200",
                    "http://localhost:3000",
                    "http://localhost:3001",
                    "http://localhost:3002",
                    "https://localhost:3000",
                    "https://localhost:3001",
                    "https://localhost:3002",
                    "http://localhost:5173",
                    "http://localhost:5174",
                    "http://localhost:5175",
                    "https://localhost:5173",
                    "https://localhost:5174",
                    "https://localhost:5175",
                    "https://supplifyhubdashboard.netlify.app",
                    "https://supplifyhub-frontend.netlify.app"
                    )
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });
            //service.
            builder.Services.AddScoped<CleanupService>();
            builder.Services.AddScoped<SubscriptionService>();

            // Register the hosted background service.
            builder.Services.AddHostedService<ScheduledTokenCleanup>();
            builder.Services.AddHostedService<ScheduledForSubscriptionPlan>();
            // Add Hangfire services
            builder.Services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(option =>
                {
                    option.UseNpgsqlConnection(builder.Configuration.GetConnectionString("con"));
                }));

            //Add the processing server
            builder.Services.AddHangfireServer();

            var app = builder.Build();

            app.UseSwagger();
            //app.UseSwaggerUI();
            //app.UseSwaggerUI(options =>
            //{
            //    // Essential: Specifies which Swagger JSON endpoint the UI should display.
            //    options.SwaggerEndpoint("/swagger/v1/swagger.json", "B2BPlatform v1");
            //});
            // Configure HTTP pipeline
            //if (app.Environment.IsDevelopment())
            //{

            //}

            app.UseHangfireDashboard();
            app.UseHttpsRedirection();
            app.UseRouting();// Essential for CORS

            app.UseCors("MyPolicy");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.Run();
        }
    }
}
