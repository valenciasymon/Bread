
using Bread.API.Application.Contracts;
using Bread.API.Application.Services;
using Bread.API.CachePolicy;
using Bread.API.Common.Constants;
using Bread.API.Common.Models.Config;
using Bread.API.Domain;
using Bread.API.Middleware;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace Bread.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //boostrap logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
                .CreateBootstrapLogger();
                
            try
            {
                Log.Information("Starting Bread.API");

                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                );

                // Add services to the container.

                //strconn
                var strconn = builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("DefaultConnection is not Found");
                //db context
                builder.Services.AddDbContextPool<BreadDbContext>(options =>
                {
                    options.UseSqlServer(strconn, sqlOptions =>
                    {
                        sqlOptions.CommandTimeout(30);
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null);
                    });

                    if (builder.Environment.IsDevelopment())
                    {
                        options.EnableDetailedErrors();
                        options.EnableSensitiveDataLogging();
                    }
                }, poolSize: 128);

                builder.Services.AddDbContext<AuthDbContext>(options => options.UseSqlServer(strconn));

                //identity
                builder.Services.AddIdentityCore<ApplicationUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                })
                    .AddRoles<IdentityRole>()
                    .AddEntityFrameworkStores<AuthDbContext>();

                //jwt ioptions
                builder.Services.Configure<JwtSettings>
                    (builder.Configuration.GetSection("JwtSettings"));

                JwtSettings jwtSettings = builder.Configuration.GetSection("JwtSettings")
                    .Get<JwtSettings>() ?? new JwtSettings();


                if (string.IsNullOrWhiteSpace(jwtSettings.Key))
                {
                    Log.Fatal("JwtSettings:Key is Not Configured");

                    throw new InvalidOperationException("JwtSetting:Key is Not Configured");
                }

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtDefaults.AuthenticationScheme;
                })
                    .AddJwtBearer(options =>
                    {
                        options.IncludeErrorDetails = true;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = jwtSettings.Issuer,
                            ValidAudience = jwtSettings.Audience,
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwtSettings.Key)),
                            ClockSkew = TimeSpan.Zero
                        };


                    });

                builder.Services.AddAuthorization();

                //services
                builder.Services.AddHttpContextAccessor();
                builder.Services.AddScoped<ICategoriesServices, CategoriesServices>();
                builder.Services.AddScoped<IUserServices, UserServices>();
                builder.Services.AddScoped<ITransactionsServices, TransactionsServices>();
                builder.Services.AddScoped<IBudgetsServices, BudgetsServices>();
                builder.Services.AddScoped<IDashBoardServices, DashBoardServices>();
                builder.Services.AddScoped<IAdminDashBoardServices, AdminDashBoardServices>();

                builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

                //output Cache
                builder.Services.AddOutputCache(options =>
                {
                    options.AddPolicy(CacheConstants.AuthenticatedUserCachingPolicy, builder =>
                    {
                        builder.AddPolicy<AuthenticatedUserCachingPolicy>()
                        .SetCacheKeyPrefix(CacheConstants.AuthenticatedUserCachingPolicyTag);
                    }, true);
                });

                //rate limiter
                builder.Services.AddRateLimiter(options =>
                {
                    //fixed
                    options.AddFixedWindowLimiter(RateLimitingConstants.FixedPolicy, fixedOptions =>
                    {
                        fixedOptions.Window = TimeSpan.FromMinutes(1);
                        fixedOptions.PermitLimit = 50;
                        fixedOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                        fixedOptions.QueueLimit = 0;
                    });

                    //sliding
                    options.AddPolicy(RateLimitingConstants.PerUserPolicy, context =>
                    {
                        string userName = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                            ?? RateLimitingConstants.Anon;

                        return RateLimitPartition.GetSlidingWindowLimiter(userName,
                            _ => new SlidingWindowRateLimiterOptions
                            {
                                Window = TimeSpan.FromMinutes(1),
                                PermitLimit = 50,
                                SegmentsPerWindow = 5,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 5
                            });
                    });

                    //strict limiter
                    options.AddPolicy(RateLimitingConstants.StrictPolicy, context =>
                    {
                        string ipAddress = context.Connection.RemoteIpAddress?.ToString()
                            ?? RateLimitingConstants.Anon;

                        return RateLimitPartition.GetFixedWindowLimiter(ipAddress,
                            _ => new FixedWindowRateLimiterOptions
                            {
                                Window = TimeSpan.FromMinutes(1),
                                PermitLimit = 10,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 0
                            });
                    });


                    //global limiter 
                    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    {
                        string ipAddress = context.Connection.RemoteIpAddress?.ToString()
                            ?? RateLimitingConstants.Unknown;

                        return RateLimitPartition.GetFixedWindowLimiter(ipAddress,
                            _ => new FixedWindowRateLimiterOptions
                            {
                                Window = TimeSpan.FromMinutes(1),
                                PermitLimit = 200,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 5
                            });
                    });

                    //on reject
                    options.OnRejected = async (context, cancellationToken) =>
                    {
                        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        {
                            context.HttpContext.Response.Headers.RetryAfter
                                = retryAfter.TotalSeconds.ToString();
                        }

                        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                        context.HttpContext.Response.ContentType = "application/json";

                        await context.HttpContext.Response.WriteAsJsonAsync(new
                        {
                            error = "Too Many Requests",
                            message = "RateLimit Exceeded. Please Try again Later",
                            retryAfter = retryAfter.TotalSeconds
                        }, cancellationToken: cancellationToken);
                    };
                });


                //healthchecks
                builder.Services.AddHealthChecks()
                    .AddCheck("self", () => HealthCheckResult.Healthy("Application Is Running!")
                        , tags: ["api"])
                    .AddDbContextCheck<BreadDbContext>(
                        name: "Bread Database",
                        failureStatus: HealthStatus.Unhealthy,
                        tags: ["db", "sql"])
                    .AddDbContextCheck<AuthDbContext>(
                        name: "Auth Database",
                        failureStatus: HealthStatus.Unhealthy,
                        tags: ["db", "sql"]);

                builder.Services.AddHealthChecksUI(setup =>
                {
                    setup.SetEvaluationTimeInSeconds(30);
                    setup.MaximumHistoryEntriesPerEndpoint(50);
                    setup.AddHealthCheckEndpoint("Bread API", "/healthz");
                }).AddInMemoryStorage();


                builder.Services.AddControllers()
                    .AddNewtonsoftJson()
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    });

                // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
                builder.Services.AddOpenApi();

                var app = builder.Build();

                app.UseExceptionHandler();

                //logging request
                app.UseSerilogRequestLogging(options =>
                {
                    options.MessageTemplate = "Http {RequestMethod} {RequestPath} responded" +
                    " {StatusCode} in {Elapsed:0.0000}ms";

                    options.GetLevel = (httpContext, elapsed, ex) => ex != null
                        ? LogEventLevel.Error
                        : httpContext.Response.StatusCode >= 500
                            ? LogEventLevel.Error
                            : httpContext.Response.StatusCode >= 400
                                ? LogEventLevel.Warning
                                : LogEventLevel.Information;

                    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                    {
                        diagnosticContext.Set("UserName",
                            httpContext.User?.FindFirstValue("username") ?? "Anonymous");

                        diagnosticContext.Set("RemoteIp",
                            httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

                        if (httpContext.User?.Identity?.IsAuthenticated == true)
                        {
                            diagnosticContext.Set("UserId",
                                httpContext.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                                ?? "Unknown");
                        }
                    };
                });

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();
                }

                app.UseHttpsRedirection();

                //healthcheck UI
                app.MapHealthChecks("/healthz", new HealthCheckOptions
                {
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                app.MapHealthChecks("/healthz/live", new HealthCheckOptions
                {
                    Predicate = check => false
                });

                app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("db")
                });

                app.MapHealthChecksUI(options =>
                {
                    options.UIPath = "/healthchecks-ui";
                    options.ApiPath = "/healthchecks-api";
                });

                app.UseAuthentication();

                app.UseAuthorization();

                app.UseRateLimiter();
                app.UseOutputCache();

                app.MapControllers();

                Log.Information("Bread Api started Sucessfully!");

                app.Run();
            }
            catch (Exception e)
            {
                Log.Fatal(e,"Application Terminated Unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
