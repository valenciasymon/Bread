
using Bread.API.Application.Contracts;
using Bread.API.Application.Services;
using Bread.API.CachePolicy;
using Bread.API.Common.Constants;
using Bread.API.Common.Models.Config;
using Bread.API.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
            var builder = WebApplication.CreateBuilder(args);

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
                throw new InvalidOperationException("JwtSetting:Key is Not Configured");

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
                options.AddFixedWindowLimiter(RateLimitingConstants.FixedPolicy, opt =>
                {
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.PermitLimit = 50;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                //sliding
                options.AddPolicy(RateLimitingConstants.PerUserPolicy, context =>
                {
                    string userName = context.User?.Identity?.Name ?? RateLimitingConstants.Anon;

                    return RateLimitPartition.GetSlidingWindowLimiter(userName,
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromMinutes(1),
                            PermitLimit = 50,
                            SegmentsPerWindow = 6,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 5
                        });
                });

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
                        retryAfter = retryAfter.Seconds
                    },cancellationToken: cancellationToken);
                };
            });

            builder.Services.AddControllers()
                .AddNewtonsoftJson()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseRateLimiter();
            app.UseOutputCache();

            app.MapControllers();

            app.Run();
        }
    }
}
