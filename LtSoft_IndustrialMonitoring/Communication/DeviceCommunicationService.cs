using System.Collections.Concurrent;
using System.Net.Sockets;
using LtSoft_IndustrialMonitoring.Interfaces;
using LtSoft_IndustrialMonitoring.Models;

namespace LtSoft_IndustrialMonitoring.Communication
{
    /// <summary>
    /// 设备通信服务类
    /// </summary>
    public class DeviceCommunicationService : IDeviceCommunicationService
    {
        private readonly ILogger<DeviceCommunicationService> _logger;

        // 记录每个设备首次检测到失败的时间，用于一分钟确认策略
        private readonly ConcurrentDictionary<int, DateTime> _firstFailureTimestamps = new();

        public DeviceCommunicationService(ILogger<DeviceCommunicationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 检查设备是否在线
        /// 仅在该设备连续一分钟内都无法连接时才判定为离线
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<bool> CheckDeviceStatus(Device device)
        {
            try
            {
                bool reachable = false;

                using TcpClient tcpClient = new TcpClient();
                Task connectTask = tcpClient.ConnectAsync(device.DeviceIP, device.Port);
                Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(3));
                Task completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask != timeoutTask)
                {
                    // connectTask 已完成, 检查连接状态
                    reachable = tcpClient.Connected;
                }
                if (reachable)
                {
                    // 如果之前有记录的首次失败时间, 清除
                    _firstFailureTimestamps.TryRemove(device.Id, out _);
                    return true;
                }

                // 本次检测不可达: 检查是否已存在首次失败时间
                DateTime now = DateTime.Now;
                if (_firstFailureTimestamps.TryGetValue(device.Id, out DateTime firstFail))
                {
                    // 如果首次失败时间已经过去超过1分钟, 则判定为离线
                    if ((now - firstFail) >= TimeSpan.FromMinutes(1))
                    {
                        _logger.LogWarning($"设备在1分钟内持续不可达，标记为离线: {device.BaseName} ({device.DeviceIP}:{device.Port})");
                        return false;
                    }
                    else
                    {
                        // 仍在确认窗口内, 不改变在线状态
                        return true;
                    }
                }
                else
                {
                    // 记录首次失败时间, 开始1分钟确认窗口
                    _firstFailureTimestamps[device.Id] = now;
                    _logger.LogInformation($"首次检测到设备不可达，开始1分钟确认窗口: {device.BaseName} ({device.DeviceIP}:{device.Port})");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking status for device {device.BaseName}");
                // 不要在异常时立即认为离线, 保持原有状态, 除非此前已经在失败窗口超过1分钟
                DateTime now = DateTime.Now;
                if (_firstFailureTimestamps.TryGetValue(device.Id, out DateTime firstFail))
                {
                    if ((now - firstFail) >= TimeSpan.FromMinutes(1))
                    {
                        _logger.LogWarning($"设备在1分钟内持续异常，标记为离线: {device.BaseName} ({device.DeviceIP}:{device.Port})");
                        return false;
                    }
                    return true;
                }
                else
                {
                    _firstFailureTimestamps[device.Id] = now;
                    return true;
                }
            }
        }

        /// <summary>
        /// 这里实现与上位机的自定义通信协议
        /// 发送简单文本命令并接收响应
        /// </summary>
        /// <param name="device"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<string> SendCommandToDevice(Device device, string command)
        {
            try
            {
                using TcpClient tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(device.DeviceIP, device.Port);

                using NetworkStream stream = tcpClient.GetStream();
                using StreamWriter writer = new StreamWriter(stream);
                using StreamReader reader = new StreamReader(stream);

                await writer.WriteLineAsync(command);
                await writer.FlushAsync();

                string? response = await reader.ReadLineAsync();
                return response ?? "No response";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending command to device {device.BaseName}");
                return $"ERROR: {ex.Message}";
            }
        }
    }
}