using LtSoft_IndustrialMonitoring.Models;

namespace LtSoft_IndustrialMonitoring.Interfaces
{
    /// <summary>
    /// 设备管理服务接口
    /// </summary>
    public interface IDeviceService
    {
        Task<List<Device>> GetAllDevicesAsync();
        Task<Device?> GetDeviceByIdAsync(int id);
        Task<Device> AddDeviceAsync(Device device);
        Task<bool> UpdateDeviceAsync(int id, Device device);
        Task<bool> DeleteDeviceAsync(int id);
        Task<bool> CheckDeviceStatusAsync(int deviceId);
        Task<List<DeviceStatus>> GetAllDevicesStatusAsync();
    }
}
