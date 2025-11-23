using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCS.Server.Hubs;
using RCS.Server.Services;

namespace RCS.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // --- 1. Cấu hình SignalR ---
            // Tăng giới hạn tin nhắn lên 10MB để nhận ảnh Screenshot/Webcam
            builder.Services.AddSignalR(options => {
                options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
            });

            // --- 2. Cấu hình CORS ---
            // Cho phép Client HTML (Web Frontend) kết nối vào
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowClient", policy =>
                {
                    // Thay đổi cổng 5500 nếu Live Server của bạn chạy cổng khác
                    // Cho phép cả localhost và 127.0.0.1 để tránh lỗi trình duyệt
                    policy.WithOrigins("http://localhost:5500", "http://127.0.0.1:5500")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // --- 3. Đăng ký Services (Dependency Injection) ---
            builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();
            builder.Services.AddSingleton<AgentCommandService>();

            // --- 4. Build App ---
            var app = builder.Build();

            // --- 5. Middleware Pipeline ---
            app.UseRouting();
            app.UseCors("AllowClient");

            // Map các Hub SignalR
            app.MapHub<ClientHub>("/clienthub");
            app.MapHub<AgentHub>("/agenthub");

            // Test endpoint để kiểm tra Server sống hay chết
            app.MapGet("/", () => "RCS Server is running...");

            // Chạy ứng dụng
            app.Run();
        }
    }
}