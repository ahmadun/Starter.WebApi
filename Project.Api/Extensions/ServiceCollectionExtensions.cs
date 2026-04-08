using System.Data;
using Dapper;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Application.Services;
using Project.Domain.Entities;
using Project.Infrastructure.Data;
using Project.Infrastructure.Mapping;
using Project.Infrastructure.Repositories;
using System.Text;

namespace Project.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        DapperTypeMapRegistrar.Register(
            typeof(Tenant),
            typeof(Member),
            typeof(User),
            typeof(LoanProduct),
            typeof(SavingsProduct),
            typeof(ProductCategory),
            typeof(Supplier),
            typeof(Product),
            typeof(Loan),
            typeof(LoanInstallmentSchedule),
            typeof(Sale),
            typeof(PurchaseReceipt),
            typeof(StockAdjustment),
            typeof(PasswordResetToken));

        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());

        services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IMemberPortalRepository, MemberPortalRepository>();
        services.AddScoped<ILoanProductRepository, LoanProductRepository>();
        services.AddScoped<ISavingsProductRepository, SavingsProductRepository>();
        services.AddScoped<ISavingsTransactionRepository, SavingsTransactionRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IReportingRepository, ReportingRepository>();
        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IMemberPortalService, MemberPortalService>();
        services.AddScoped<IRequestApprovalService, RequestApprovalService>();
        services.AddScoped<ILoanProductService, LoanProductService>();
        services.AddScoped<ISavingsProductService, SavingsProductService>();
        services.AddScoped<ISavingsTransactionService, SavingsTransactionService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<ILoanService, LoanService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
        return services;
    }

    public static IServiceCollection AddApprovalNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApprovalNotificationSettings>(configuration.GetSection(ApprovalNotificationSettings.SectionName));
        services.Configure<CronDispatchSettings>(configuration.GetSection(CronDispatchSettings.SectionName));
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings section is missing from appsettings.json.");

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "KSP POS Waserda API",
                Version = "v1",
                Description = "Cooperative KSP + POS Waserda REST API",
            });
        });

        return services;
    }

    public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value) => parameter.Value = value.ToDateTime(TimeOnly.MinValue);
        public override DateOnly Parse(object value) => DateOnly.FromDateTime((DateTime)value);
    }

    public sealed class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
    {
        public override void SetValue(IDbDataParameter parameter, TimeOnly value)
        {
            parameter.DbType = DbType.Time;
            parameter.Value = value.ToTimeSpan();
        }

        public override TimeOnly Parse(object value) => value switch
        {
            TimeSpan timeSpan => TimeOnly.FromTimeSpan(timeSpan),
            DateTime dateTime => TimeOnly.FromDateTime(dateTime),
            _ => throw new DataException($"Cannot convert {value.GetType()} to TimeOnly")
        };
    }
}
