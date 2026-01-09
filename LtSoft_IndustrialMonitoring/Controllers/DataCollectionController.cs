using LtSoft_IndustrialMonitoring.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace LtSoft_IndustrialMonitoring.Controllers
{
    /// <summary>
    /// 数据采集控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DataCollectionController : ControllerBase
    {
        private readonly IDeviceService _deviceService;
        private readonly IDeviceCommunicationService _communicationService;

        public DataCollectionController(IDeviceService deviceService, IDeviceCommunicationService communicationService)
        {
            _deviceService = deviceService;
            _communicationService = communicationService;
        }

        //[HttpPost("{deviceId}/start")]
        //public async Task<ActionResult> StartCollection(int deviceId)
        //{
        //    Models.Device? device = await _deviceService.GetDeviceByIdAsync(deviceId);

        //    if (device == null)
        //        return NotFound();

        //    bool result = await _communicationService.StartDataCollection(device);

        //    if (!result)
        //        return BadRequest("Failed to start data collection");

        //    return Ok(new { message = "Data collection started successfully" });
        //}

        //[HttpPost("{deviceId}/stop")]
        //public async Task<ActionResult> StopCollection(int deviceId)
        //{
        //    Models.Device? device = await _deviceService.GetDeviceByIdAsync(deviceId);

        //    if (device == null)
        //        return NotFound();

        //    bool result = await _communicationService.StopDataCollection(device);

        //    if (!result)
        //        return BadRequest("Failed to stop data collection");

        //    return Ok(new { message = "Data collection stopped successfully" });
        //}
    }
}
