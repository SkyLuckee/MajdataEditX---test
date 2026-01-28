using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;

namespace MajdataEdit.ChartShare;

public static class ChartServer
{
    public static WebApplication? App { get; set; }

    // 启动服务器
    public static async Task StartAsync(HubDataService cds, string path, int port = 8014)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(port, lOptions =>
            {
                lOptions.UseHttps(httpsOptions =>
                {
                    httpsOptions.ServerCertificate = GetCert();
                });
            });
        });

        var chartData = cds;
        builder.Services.AddSingleton(chartData);

        // 注册 SignalR 服务
        builder.Services.AddSignalR(options =>
        {
            options.MaximumReceiveMessageSize = 1024 * 1024 * 8; // 8 MB
        });

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyHeader()
                      .AllowAnyMethod()
                      .SetIsOriginAllowed(_ => true) // 允许所有来源
                      .AllowCredentials();

            });
        });

        App = builder.Build();

        App.UseCors();
        App.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        App.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(path),
            RequestPath = "/chartFiles"
        });

        // 映射 Hub
        App.MapHub<ChartHub>("/chartHub");

        await App.StartAsync();
    }

    // 停止服务器
    public static async Task StopAsync()
    {
        if (App != null)
        {
            await App.StopAsync();
            await App.DisposeAsync();
            App = null;
        }
    }

    private static X509Certificate2 GetCert()
    {
        string certPath = "server.pfx";
        string password = "chart_share";
        // 从文件加载时，必须指定存储标志，否则 Kestrel 无法访问私钥
        return new X509Certificate2(certPath, password,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
    }
}
