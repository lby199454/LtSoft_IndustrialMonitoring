using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using LtSoft_IndustrialMonitoring.Interfaces;
using LtSoft_IndustrialMonitoring.Models;

namespace LtSoft_IndustrialMonitoring.Communication
{
	/// <summary>
	/// 通信服务类
	/// </summary>
	public class DeviceCommunicationService : IDeviceCommunicationService
	{
		private const int RecoverySuccessThreshold = 2; // 离线设备恢复在线所需连续成功次数
		private const int OnlineProbeAttempts = 3;      // 在线设备探测失败时允许的重试次数
		private static readonly TimeSpan FailureConfirmWindow = TimeSpan.FromMinutes(1.5); // 在线设备持续失败超过此时间判离线
		private static readonly TimeSpan StatusConnectTimeout = TimeSpan.FromSeconds(5); // 设备状态连接超时时间
		private static readonly TimeSpan OnlineProbeRetryDelay = TimeSpan.FromMilliseconds(500); // 在线设备探测重试延迟等待

		private readonly ILogger<DeviceCommunicationService> _logger;

		// 记录每个设备首次检测到失败的时间，用于一分钟确认策略
		private readonly ConcurrentDictionary<int, DateTime> _firstFailureTimestamps = new();
		// 记录每个离线设备连续探测成功次数，用于恢复在线防抖
		private readonly ConcurrentDictionary<int, int> _recoverySuccessCounts = new();
		// 同一设备的探测请求串行化，避免后台服务和前端接口同时建立多个TCP连接
		private readonly ConcurrentDictionary<int, SemaphoreSlim> _deviceCheckLocks = new();

		// 日志时间前缀方法
		private static string Timestamp() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

		public DeviceCommunicationService(ILogger<DeviceCommunicationService> logger)
		{
			_logger = logger;
		}

		/// <summary>
		/// 检查设备是否在线
		/// 在线设备：持续失败超过1.5分钟才判离线
		/// 离线设备：连续3次成功才恢复在线
		/// </summary>
		/// <param name="device"></param>
		/// <returns></returns>
		public async Task<bool> CheckDeviceStatus(Device device)
		{
			SemaphoreSlim deviceLock = _deviceCheckLocks.GetOrAdd(device.Id, _ => new SemaphoreSlim(1, 1));
			await deviceLock.WaitAsync();

			try
			{
				TcpProbeSummary probe = await ProbeReachabilityAsync(device, device.IsOnline);
				bool reachable = probe.Success;
				DateTime now = DateTime.Now;

				// 在线状态下：按“持续失败超过1分钟”判离线
				if (device.IsOnline)
				{
					if (reachable)
					{
						_firstFailureTimestamps.TryRemove(device.Id, out _);
						_recoverySuccessCounts.TryRemove(device.Id, out _);
						return true;
					}

					if (_firstFailureTimestamps.TryGetValue(device.Id, out DateTime firstFail))
					{
						if ((now - firstFail) >= FailureConfirmWindow)
						{
							_firstFailureTimestamps.TryRemove(device.Id, out _);
							_recoverySuccessCounts.TryRemove(device.Id, out _);
                           _logger.LogWarning("[{Timestamp}] DeviceOffline DeviceId={DeviceId} IP={IP} Name={Name} ElapsedMs={ElapsedMs} Reason={Reason}",
								Timestamp(), device.Id, device.IP, device.Name, probe.ElapsedMilliseconds, probe.FailureReason);
							return false;
						}

						// 仍在失败确认窗口内，保持在线
						return true;
					}

					// 首次失败，开始1分钟确认窗口
					_firstFailureTimestamps[device.Id] = now;
                           _logger.LogInformation("[{Timestamp}] FirstFailure DeviceId={DeviceId} IP={IP} Name={Name} Reason={Reason} ElapsedMs={ElapsedMs}",
								Timestamp(), device.Id, device.IP, device.Name, probe.FailureReason, probe.ElapsedMilliseconds);
					return true;
				}

				// 离线状态下：按“连续3次成功”恢复在线
				if (reachable)
				{
					int successCount = _recoverySuccessCounts.AddOrUpdate(device.Id, 1, (_, current) => current + 1);
					if (successCount >= RecoverySuccessThreshold)
					{
						_recoverySuccessCounts.TryRemove(device.Id, out _);
						_firstFailureTimestamps.TryRemove(device.Id, out _);
                               _logger.LogInformation("[{Timestamp}] DeviceRecovered DeviceId={DeviceId} IP={IP} Name={Name} ElapsedMs={ElapsedMs}",
									Timestamp(), device.Id, device.IP, device.Name, probe.ElapsedMilliseconds);
						return true;
					}

                               _logger.LogInformation("[{Timestamp}] RecoveryProgress DeviceId={DeviceId} IP={IP} Name={Name} SuccessCount={SuccessCount} Threshold={Threshold} ElapsedMs={ElapsedMs}",
									Timestamp(), device.Id, device.IP, device.Name, successCount, RecoverySuccessThreshold, probe.ElapsedMilliseconds);
					return false;
				}

				// 离线状态下出现失败，恢复成功计数清零
				if (_recoverySuccessCounts.TryRemove(device.Id, out int previousSuccessCount) && previousSuccessCount > 0)
				{
                               _logger.LogInformation("[{Timestamp}] RecoveryReset DeviceId={DeviceId} IP={IP} Name={Name} Reason={Reason} ElapsedMs={ElapsedMs}",
									Timestamp(), device.Id, device.IP, device.Name, probe.FailureReason, probe.ElapsedMilliseconds);
				}
				return false;
			}
			catch (Exception ex)
			{
                            _logger.LogError(ex, $"[{Timestamp()}] Error checking status for device DeviceId:{device.Id} {device.Name}");

				// 异常按失败处理，但在线设备仍遵守“超过1分钟才离线”规则
				DateTime now = DateTime.Now;
				if (device.IsOnline)
				{
					if (_firstFailureTimestamps.TryGetValue(device.Id, out DateTime firstFail))
					{
						if ((now - firstFail) >= FailureConfirmWindow)
						{
							_firstFailureTimestamps.TryRemove(device.Id, out _);
							_recoverySuccessCounts.TryRemove(device.Id, out _);
                             _logger.LogWarning($"[{Timestamp()}] DeviceId:{device.Id} 设备持续异常超过1分钟，标记为离线: {device.Name} ({device.IP}:{device.Port}), 异常: {ex.GetType().Name}: {ex.Message}");
							return false;
						}

						return true;
					}

					_firstFailureTimestamps[device.Id] = now;
                            _logger.LogWarning($"[{Timestamp()}] DeviceId:{device.Id} 设备状态探测异常，开始1分钟确认窗口: {device.Name} ({device.IP}:{device.Port}), 异常: {ex.GetType().Name}: {ex.Message}");
					return true;
				}

				// 离线状态异常，保持离线并重置恢复成功计数
				_recoverySuccessCounts.TryRemove(device.Id, out _);
				return false;
			}
			finally
			{
				deviceLock.Release();
			}
		}

		/// <summary>
		/// 探测设备可达性，在线设备允许重试以减少误判，离线设备不重试以快速响应状态变化
		/// </summary>
		/// <param name="device"></param>
		/// <param name="allowRetry"></param>
		/// <returns></returns>
		private async Task<TcpProbeSummary> ProbeReachabilityAsync(Device device, bool allowRetry)
		{
			int attempts = allowRetry ? OnlineProbeAttempts : 1;
			TcpProbeResult lastFailure = new TcpProbeResult(false, 0, "Unknown");

			for (int attempt = 1; attempt <= attempts; attempt++)
			{
				TcpProbeResult result = await TryConnectAsync(device, StatusConnectTimeout);
				if (result.Success)
				{
					if (attempt > 1)
					{
                            _logger.LogInformation($"[{Timestamp()}] DeviceId:{device.Id} TCP重试连接成功({attempt}/{attempts}): {device.Name} ({device.IP}:{device.Port}), 耗时: {result.ElapsedMilliseconds}ms");
					}

					return new TcpProbeSummary(true, result.ElapsedMilliseconds, string.Empty);
				}

				lastFailure = result;
                          _logger.LogWarning($"[{Timestamp()}] DeviceId:{device.Id} TCP探测失败({attempt}/{attempts}): {device.Name} ({device.IP}:{device.Port}), 原因: {result.FailureReason}, 耗时: {result.ElapsedMilliseconds}ms");

				if (attempt < attempts)
				{
					await Task.Delay(OnlineProbeRetryDelay);
				}
			}

                      _logger.LogWarning($"[{Timestamp()}] DeviceId:{device.Id} TCP探测最终失败, 耗时: {lastFailure.ElapsedMilliseconds}ms, 原因: {lastFailure.FailureReason}");
						return new TcpProbeSummary(false, lastFailure.ElapsedMilliseconds, lastFailure.FailureReason);
		}

		/// <summary>
		/// 尝试建立TCP连接以探测设备是否可达
		/// </summary>
		/// <param name="device"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		private static async Task<TcpProbeResult> TryConnectAsync(Device device, TimeSpan timeout)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			using TcpClient tcpClient = new TcpClient
			{
				NoDelay = true
			};
			using CancellationTokenSource cts = new CancellationTokenSource(timeout);

			try
			{
				await tcpClient.ConnectAsync(device.IP, device.Port, cts.Token);
				stopwatch.Stop();

				return tcpClient.Connected
					? new TcpProbeResult(true, stopwatch.ElapsedMilliseconds, string.Empty)
					: new TcpProbeResult(false, stopwatch.ElapsedMilliseconds, "ConnectedFalse");
			}
			catch (OperationCanceledException)
			{
				stopwatch.Stop();
				return new TcpProbeResult(false, stopwatch.ElapsedMilliseconds, "ConnectTimeout");
			}
			catch (SocketException ex)
			{
				stopwatch.Stop();
				return new TcpProbeResult(false, stopwatch.ElapsedMilliseconds, $"SocketError:{ex.SocketErrorCode}");
			}
			catch (Exception ex)
			{
				stopwatch.Stop();
				return new TcpProbeResult(false, stopwatch.ElapsedMilliseconds, $"{ex.GetType().Name}: {ex.Message}");
			}
		}

		/// <summary>
		/// 备份原始方法 设备在线检测逻辑（保留以便快速回退
		/// </summary>
		/// <param name="device"></param>
		/// <returns></returns>
		private async Task<bool> CheckDeviceStatus_Backup(Device device)
		{
			try
			{
				bool reachable = false;

				using TcpClient tcpClient = new TcpClient();
				Task connectTask = tcpClient.ConnectAsync(device.IP, device.Port);
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
						_logger.LogWarning($"设备在1分钟内持续不可达，标记为离线: {device.Name} ({device.IP}:{device.Port})");
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
					_logger.LogInformation($"首次检测到设备不可达，开始1分钟确认窗口: {device.Name} ({device.IP}:{device.Port})");
					return true;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error checking status for device {device.Name}");
				// 不要在异常时立即认为离线, 保持原有状态, 除非此前已经在失败窗口超过1分钟
				DateTime now = DateTime.Now;
				if (_firstFailureTimestamps.TryGetValue(device.Id, out DateTime firstFail))
				{
					if ((now - firstFail) >= TimeSpan.FromMinutes(1))
					{
						_logger.LogWarning($"设备在1分钟内持续异常，标记为离线: {device.Name} ({device.IP}:{device.Port})");
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
				await tcpClient.ConnectAsync(device.IP, device.Port);

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
				_logger.LogError(ex, $"Error sending command to device {device.Name}");
				return $"ERROR: {ex.Message}";
			}
		}

		/// <summary>
		/// TCP探测结果记录结构
		/// </summary>
		/// <param name="Success"></param>
		/// <param name="ElapsedMilliseconds"></param>
		/// <param name="FailureReason"></param>
		private sealed record TcpProbeResult(bool Success, long ElapsedMilliseconds, string FailureReason);

		/// <summary>
		/// TCP探测总结结果结构（包含重试逻辑后的最终结果）
		/// </summary>
		/// <param name="Success"></param>
		/// <param name="ElapsedMilliseconds"></param>
		/// <param name="FailureReason"></param>
		private sealed record TcpProbeSummary(bool Success, long ElapsedMilliseconds, string FailureReason);
	}
}
