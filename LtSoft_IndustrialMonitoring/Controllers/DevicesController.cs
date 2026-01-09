using LtSoft_IndustrialMonitoring.Data;
using LtSoft_IndustrialMonitoring.Interfaces;
using LtSoft_IndustrialMonitoring.Models;

using Microsoft.AspNetCore.Mvc;

namespace LtSoft_IndustrialMonitoring.Controllers
{
    /// <summary>
    /// 设备管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly IDeviceService _deviceService;
        private readonly IndustrialMonitoringContext _context;

        public DevicesController(IDeviceService deviceService, IndustrialMonitoringContext context)
        {
            _deviceService = deviceService;
            _context = context;
        }

        /// <summary>
        /// 获取所有已注册的设备列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Device>>> GetDevices()
        {
            List<Device> devices = await _deviceService.GetAllDevicesAsync();
            return Ok(devices);
        }

        /// <summary>
        /// 根据设备 ID 获取单个设备的详细信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Device>> GetDevice(int id)
        {
            Device? device = await _deviceService.GetDeviceByIdAsync(id);

            if (device == null)
                return NotFound();

            return Ok(device);
        }

        /// <summary>
        /// 新增一个设备记录到系统
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<Device>> PostDevice(Device device)
        {
            Device newDevice = await _deviceService.AddDeviceAsync(device);
            return CreatedAtAction(nameof(GetDevice), new { id = newDevice.Id }, newDevice);
        }

        /// <summary>
        /// 更新指定设备 ID 的设备信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDevice(int id, Device device)
        {
            if (id != device.Id)
                return BadRequest();

            bool result = await _deviceService.UpdateDeviceAsync(id, device);

            if (!result)
                return NotFound();

            return NoContent();
        }

        /// <summary>
        /// 根据设备 ID 删除一个设备记录
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDevice(int id)
        {
            bool result = await _deviceService.DeleteDeviceAsync(id);

            if (!result)
                return NotFound();

            return NoContent();
        }

        /// <summary>
        /// 检查指定设备 ID 的当前在线状态（是否可连接）
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}/status")]
        public async Task<ActionResult<bool>> GetDeviceStatus(int id)
        {
            bool status = await _deviceService.CheckDeviceStatusAsync(id);
            return Ok(status);
        }

        /// <summary>
        /// 批量检查所有设备的当前在线状态
        /// </summary>
        /// <returns></returns>
        [HttpGet("status")]
        public async Task<ActionResult<IEnumerable<DeviceStatus>>> GetAllDevicesStatus()
        {
            List<DeviceStatus> statuses = await _deviceService.GetAllDevicesStatusAsync();
            return Ok(statuses);
        }
    }
}