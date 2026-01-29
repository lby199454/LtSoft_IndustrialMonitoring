using LtSoft_IndustrialMonitoring.Interfaces;
using LtSoft_IndustrialMonitoring.Models;

namespace LtSoft_IndustrialMonitoring.Services
{
    /// <summary>
    /// 后台服务, 定期检查设备状态并更新数据库
    /// </summary>
    public class DeviceStatusBackgroundService : BackgroundService
    {
        private readonly ILogger<DeviceStatusBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        public DeviceStatusBackgroundService(ILogger<DeviceStatusBackgroundService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 执行后台任务
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("设备状态后台服务正在启动.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 创建一个新的作用域以获取Scoped服务
                    using IServiceScope scope = _serviceProvider.CreateScope();
                    IDeviceService deviceService = scope.ServiceProvider.GetRequiredService<IDeviceService>();
                    List<Device> devices = await deviceService.GetAllDevicesAsync();
                    // 存储需要发送告警的设备状态变化信息
                    List<string> sendAlertDeviceStatus = new List<string>();

                    foreach (Device device in devices)
                    {
                        bool previousStatus = device.IsOnline;
                        await deviceService.CheckDeviceStatusAsync(device.Id);

                        if (previousStatus && !device.IsOnline)
                        {
                            sendAlertDeviceStatus.Add($"⚠️【设备掉线通知】:\n❌Name: {device.Name}\r\n IP: {device.IP}:{device.Port}\r\n Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n" +
                                $"💡请相关人员立即排查原因");
                            _logger.LogWarning($"设备掉线: {device.Name}, IP: {device.IP}:{device.Port}");
                        }
                        else if (!previousStatus && device.IsOnline)
                        {
                            sendAlertDeviceStatus.Add($"✅【设备恢复通知】:\n✔Name: {device.Name}\r\n IP: {device.IP}:{device.Port}\r\n Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                            _logger.LogInformation($"设备恢复在线: {device.Name}, IP: {device.IP}:{device.Port}");
                        }

                        // 添加短暂延迟，避免同时检查所有设备
                        await Task.Delay(1000, stoppingToken);
                    }

                    if (sendAlertDeviceStatus.Count != 0)
                    {
                        await SendDingTalkAlertAsync(sendAlertDeviceStatus);
                    }

                    // 在设备状态检查循环最后添加
                    if (devices.Count > 0)
                    {
                        List<DeviceStatus> statusList = devices.Select(d => new DeviceStatus
                        {
                            DeviceId = d.Id,
                            IsOnline = d.IsOnline,
                            LastChecked = d.LastCommunication
                        }).ToList();

                        await WebSocketManager.BroadcastAsync(statusList);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "检查设备状态时出错!");
                }
                await Task.Delay(_checkInterval, stoppingToken);
            }
            _logger.LogInformation("设备状态后台服务正在停止.");
        }

        private async Task SendDingTalkAlertAsync(List<string> messages)
        {
            try
            {
                // 获取多个 Webhook URL
                IConfiguration configuration = _serviceProvider.GetRequiredService<IConfiguration>();
                string? webhookUrl1 = configuration.GetValue<string>("DingTalk:WebhookUrl1");
                string? webhookUrl2 = configuration.GetValue<string>("DingTalk:WebhookUrl2");

                if (string.IsNullOrEmpty(webhookUrl1) && string.IsNullOrEmpty(webhookUrl2))
                {
                    _logger.LogWarning("钉钉Webhook未配置，跳过告警发送");
                    return;
                }

                string content = string.Join("\n", messages);
                var message = new
                {
                    msgtype = "text",
                    text = new
                    {
                        content
                    }
                };

                // 定义一个方法发送消息
                async Task SendToWebhook(string? webhookUrl)
                {
                    if (!string.IsNullOrEmpty(webhookUrl))
                    {
                        using HttpClient client = new HttpClient();
                        HttpResponseMessage response = await client.PostAsJsonAsync(webhookUrl, message);
                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogError($"钉钉告警发送失败: {response.StatusCode}, Webhook: {webhookUrl}");
                        }
                        else
                        {
                            _logger.LogInformation($"钉钉告警发送成功! Webhook: {webhookUrl}");
                        }
                    }
                }

                // 依次发送到 WebhookUrl1 和 WebhookUrl2
                await Task.WhenAll(SendToWebhook(webhookUrl1), SendToWebhook(webhookUrl2));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "钉钉告警发送异常");
            }
        }
    }
}
