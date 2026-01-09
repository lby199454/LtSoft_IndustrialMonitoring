using LtSoft_IndustrialMonitoring.Models;

namespace LtSoft_IndustrialMonitoring.Interfaces
{
    /// <summary>
    /// 设备通信服务接口
    /// </summary>
    public interface IDeviceCommunicationService
    {
        Task<bool> CheckDeviceStatus(Device device);

        Task<string> SendCommandToDevice(Device device, string command);
        
        //Task<bool> StartDataCollection(Device device);

        //Task<bool> StopDataCollection(Device device);
    }
}
