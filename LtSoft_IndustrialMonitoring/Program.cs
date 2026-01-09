using System.Net.WebSockets;
using System.Text;
using LtSoft_IndustrialMonitoring.Communication;
using LtSoft_IndustrialMonitoring.Data;
using LtSoft_IndustrialMonitoring.Interfaces;
using LtSoft_IndustrialMonitoring.Models;
using LtSoft_IndustrialMonitoring.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using WebSocketManager = LtSoft_IndustrialMonitoring.Services.WebSocketManager;

internal class Program
{
    private static void Main(string[] args)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        string host = builder.Configuration["Host"] ?? "http://0.0.0.0:7070";

        // 添加服务到容器
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // 配置连接字符串
        string? devicesMonitorConnectionString = builder.Configuration["DevicesMonitoringContext"];
        string? tempDataConnectionString = builder.Configuration["TemperatureDataContext"];
        string? counterDataConnectionString = builder.Configuration["CounterDataContext"];

        // 验证连接字符串
        if (string.IsNullOrEmpty(devicesMonitorConnectionString) || string.IsNullOrEmpty(tempDataConnectionString) || string.IsNullOrEmpty(counterDataConnectionString))
        {
            throw new InvalidOperationException("MySQL连接字符串未配置");
        }

        // 添加设备监控 数据库上下文
        builder.Services.AddDbContext<IndustrialMonitoringContext>(options =>
            options.UseMySql(devicesMonitorConnectionString,
            new MySqlServerVersion(new Version(8, 0, 36))));

        // 添加温度数据 数据库上下文
        builder.Services.AddDbContext<TemperatureDataContext>(options =>
            options.UseMySql(/*builder.Configuration.GetConnectionString*/(tempDataConnectionString),
            new MySqlServerVersion(new Version(8, 0, 36))));

        // 添加筛分计数 数据库上下文
        builder.Services.AddDbContext<CounterDataContext>(options =>
            options.UseMySql(/*builder.Configuration.GetConnectionString*/(counterDataConnectionString),
            new MySqlServerVersion(new Version(8, 0, 36))));

        // 注册温度数据导出、设备监控、筛分计数服务    
        builder.Services.AddSingleton<IDeviceCommunicationService, DeviceCommunicationService>();
        builder.Services.AddScoped<IDeviceService, DeviceService>();
        builder.Services.AddScoped<ITemperatureDataService, TemperatureDataService>();
        builder.Services.AddScoped<ICounterDataService, CounterDataService>();
        builder.Services.AddHostedService<DeviceStatusBackgroundService>();

        // 添加CORS策略
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigin", policy =>
            {
                policy.WithOrigins("http://211.137.106.175:82", "http://localhost:82")
                      .AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // 添加JWT身份验证
        JwtSettings? jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
        builder.Services.Configure<List<UserConfig>>(builder.Configuration.GetSection("Users"));
        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
        builder.Services.AddAuthentication(options =>
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
            };
        });

        builder.Services.AddControllers();
        builder.Services.AddScoped<IAuthService, AuthService>();

        WebApplication app = builder.Build();

        // 配置HTTP请求管道
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        //IServiceScope scope = app.Services.CreateScope();
        //IndustrialMonitoringContext dbContext = scope.ServiceProvider.GetRequiredService<IndustrialMonitoringContext>();
        ////dbContext.Database.EnsureCreated();
        //await SeedData(dbContext);

        app.UseCors("AllowSpecificOrigin");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.UseWebSockets();
        app.Map("/ws/devices", async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                WebSocketManager.Add(webSocket);

                byte[] buffer = new byte[1024 * 4];
                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        WebSocketManager.Remove(webSocket);
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                    }
                }
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        });

        ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("服务正在启动，监听地址: {Host}", host);
        app.Run(host);
    }

    /// <summary>
    /// 添加初始数据的示例方法
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private static async Task SeedData(IndustrialMonitoringContext context)
    {
        //// 检查是否已有数据
        //if (!context.Devices.Any())
        {
            context.Devices.Add(new Device
            {
                BaseName = "测试写入数据库",
                DeviceIP = "127.0.0.1",
                Port = 8080,
                SqlTableName = "_tempdata",
                DeviceAddresses = new int[] { 1, 2, 3, 4, 5, 6 },
                IsOnline = false,
                LastCommunication = DateTime.Now, //DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss")
                CreatedAt = DateTime.Now
            });

            await context.SaveChangesAsync();
            Console.WriteLine("初始数据已添加");
        }
        //else
        //{
        //    Console.WriteLine("这个if得去掉");
        //}
    }
}