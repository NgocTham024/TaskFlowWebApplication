
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims; 
using System.Text;
using TaskFlowWebApplication.Data;
using TaskFlowWebApplication.Services;
using TaskFlowWebApplication.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Globalization;
namespace TaskFlowWebApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration
                .GetConnectionString("DefaultConnection");

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .ToDictionary(
                            x => x.Key,
                            x => x.Value!.Errors
                                .Select(error => error.ErrorMessage)
                                .ToArray()
                        );
                    return new BadRequestObjectResult(new
                    {
                        success = false,
                        message = "Dữ liệu không hợp lệ.",
                        errors,
                        traceId = context.HttpContext.TraceIdentifier
                    });
                };
            });


            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString)));

            // Đăng ký Services
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ITokenIssue, TokenIssue>();
            builder.Services.AddScoped<UserRoleService>();

            // Authentication - JWT
            var jwtSettings = builder.Configuration
                .GetSection("JwtSettings");

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ClockSkew = TimeSpan.Zero,

                        ValidIssuer = jwtSettings["Issuer"],
                        ValidAudience = jwtSettings["Audience"],

                        IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtSettings["SecretKey"]!)),

                        RoleClaimType = ClaimTypes.Role
                    };
            });

            // Authorization
            builder.Services.AddAuthorization();

            //  CORS 
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:5173").AllowAnyMethod().AllowAnyHeader().AllowCredentials();
                });
            });

            // Global Exception Handling 
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();
            // Rate limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy("LoginLimiter", context => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }
                    )
                );
                // > limited
                options.OnRejected = async (context, cancellationToken) =>
                {
                    var response = context.HttpContext.Response;

                    response.StatusCode =
                        StatusCodes.Status429TooManyRequests;

                    if (context.Lease.TryGetMetadata(
                        MetadataName.RetryAfter, out var retryAfter))
                    {
                        response.Headers.RetryAfter =
                            Math.Ceiling(retryAfter.TotalSeconds)
                                .ToString(CultureInfo.InvariantCulture);
                    }

                    await response.WriteAsJsonAsync(
                        new
                        {
                            success = false,
                            message = "Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau.",
                            traceId = context.HttpContext.TraceIdentifier
                        },
                        cancellationToken);
                };
            });
            var app = builder.Build();
            app.UseExceptionHandler();

            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseSwagger();
            //    app.UseSwaggerUI();
            //}

            
            // test middleware
            if (app.Environment.IsDevelopment())
            {
                app.MapGet("/api/test-error", () =>
                {
                    throw new Exception("Test Global Exception");
                });
            }
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowFrontend");
            app.UseRateLimiter();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
