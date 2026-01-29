using LtSoft_IndustrialMonitoring.Data;
using LtSoft_IndustrialMonitoring.Interfaces;
using LtSoft_IndustrialMonitoring.Models;

using Microsoft.EntityFrameworkCore;

namespace LtSoft_IndustrialMonitoring.Services
{
    /// <summary>
    /// 设备管理服务
    /// </summary>
    public class DeviceService : IDeviceService
    {
        private readonly IndustrialMonitoringContext _context;
        private readonly IDeviceCommunicationService _communicationService;

        public DeviceService(IndustrialMonitoringContext context, IDeviceCommunicationService communicationService)
        {
            _context = context;
            _communicationService = communicationService;
        }

        ~DeviceService()
        {
            _context.Dispose();
        }

        /// <summary>
        /// 获取所有已注册的设备列表
        /// </summary>
        /// <returns></returns>
        public async Task<List<Device>> GetAllDevicesAsync()
        {
            return await _context.Devices.ToListAsync();
        }

        /// <summary>
        /// 根据设备 ID 获取单个设备的详细信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Device?> GetDeviceByIdAsync(int id)
        {
            return await _context.Devices.FindAsync(id);
        }

        /// <summary>
        /// 新增一个设备记录到系统
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<Device> AddDeviceAsync(Device device)
        {
            device.Id = 0;
            device.CreatedAt = DateTime.Now;
            device.LastCommunication = DateTime.Now;

            _context.Devices.Add(device);
            await _context.SaveChangesAsync();
            return device;
        }

        /// <summary>
        /// 更新设备信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<bool> UpdateDeviceAsync(int id, Device device)
        {
            if (id != device.Id)
                return false;

            // 获取现有设备
            Device? existingDevice = await _context.Devices.FindAsync(id);
            if (existingDevice == null)
                return false;

            // 只更新允许修改的字段
            existingDevice.Name = device.Name;
            existingDevice.IP = device.IP;
            existingDevice.Port = device.Port;
            existingDevice.Type = device.Type;
            existingDevice.Remarks = device.Remarks;
            existingDevice.IsOnline = device.IsOnline;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DeviceExists(id))
                    return false;
                throw;
            }
            return true;
        }

        /// <summary>
        /// 删除指定设备 ID 的设备记录
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> DeleteDeviceAsync(int id)
        {
            Device? device = await _context.Devices.FindAsync(id);
            if (device == null)
                return false;

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// 检查设备是否在线并更新状态
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async Task<bool> CheckDeviceStatusAsync(int deviceId)
        {
            Device? device = await _context.Devices.FindAsync(deviceId);
            if (device == null)
                return false;

            bool isOnline = await _communicationService.CheckDeviceStatus(device);
            device.IsOnline = isOnline;
            device.LastCommunication = DateTime.Now;

            _context.Entry(device).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return isOnline;
        }

        /// <summary>
        /// 获取所有设备的当前状态
        /// </summary>
        /// <returns></returns>
        public async Task<List<DeviceStatus>> GetAllDevicesStatusAsync()
        {
            List<Device> devices = await _context.Devices.ToListAsync();
            List<DeviceStatus> statusList = new List<DeviceStatus>();

            foreach (Device device in devices)
            {
                bool isOnline = await _communicationService.CheckDeviceStatus(device);
                statusList.Add(new DeviceStatus
                {
                    DeviceId = device.Id,
                    IsOnline = isOnline,
                    LastChecked = DateTime.Now
                });
            }
            return statusList;
        }

        /// <summary>
        /// 检查指定设备是否存在
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool DeviceExists(int id)
        {
            return _context.Devices.Any(e => e.Id == id);
        }
    }
}
