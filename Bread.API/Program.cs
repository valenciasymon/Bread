
using Bread.API.Application.Services;
using Bread.API.Common.Constants;
using Bread.API.Common.Models.Config;
using Bread.API.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
            builder.Services.AddIdentityCore<ApplicationUser>()
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
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
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
            builder.Services.AddScoped<IUserServices, UserServices>();

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
