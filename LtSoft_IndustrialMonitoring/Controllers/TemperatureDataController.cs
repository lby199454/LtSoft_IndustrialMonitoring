using LtSoft_IndustrialMonitoring.Interfaces;
using LtSoft_IndustrialMonitoring.Models;

using Microsoft.AspNetCore.Mvc;

namespace LtSoft_IndustrialMonitoring.Controllers
{
    /// <summary>
    /// 温度数据控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TemperatureDataController : ControllerBase
    {
        private readonly ITemperatureDataService _temperatureDataService;
        private readonly ILogger<TemperatureDataController> _logger;

        public TemperatureDataController(ITemperatureDataService temperatureDataService, ILogger<TemperatureDataController> logger)
        {
            _temperatureDataService = temperatureDataService;
            _logger = logger;
        }

        [HttpGet("locations")]
        public async Task<ActionResult<List<LocationInfo>>> GetAvailableLocations()
        {
            try
            {
                List<LocationInfo> locations = await _temperatureDataService.GetAvailableLocationsAsync();
                return Ok(locations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取地点列表时发生错误");
                return StatusCode(500, "内部服务器错误");
            }
        }

        [HttpPost("filter")]
        public async Task<ActionResult<TemperatureDataResponse>> GetFilteredData([FromBody] TemperatureDataFilter filter)
        {
            try
            {
                if (string.IsNullOrEmpty(filter.TableName))
                {
                    return BadRequest("表名不能为空");
                }

                TemperatureDataResponse result = await _temperatureDataService.GetFilteredDataAsync(filter);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取筛选数据时发生错误");
                return StatusCode(500, "内部服务器错误");
            }
        }

        [HttpPost("export/csv")]
        public async Task<IActionResult> ExportToCsv([FromBody] TemperatureDataFilter filter)
        {
            try
            {
                if (string.IsNullOrEmpty(filter.TableName))
                {
                    return BadRequest("表名不能为空");
                }

                byte[] fileBytes = await _temperatureDataService.ExportToCsvAsync(filter);

                // 获取地点显示名称
                List<LocationInfo> locations = await _temperatureDataService.GetAvailableLocationsAsync();
                LocationInfo? location = locations.FirstOrDefault(l => l.TableName.Equals(filter.TableName, StringComparison.OrdinalIgnoreCase));
                string displayName = location?.DisplayName ?? filter.TableName;

                return File(fileBytes,
                    "text/csv",
                    $"{displayName}_温度数据_{DateTime.Now:yyyyMMddHHmmss}.csv");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出CSV时发生错误");
                return StatusCode(500, "内部服务器错误");
            }
        }

        [HttpPost("export/excel")]
        public async Task<IActionResult> ExportToExcel([FromBody] TemperatureDataFilter filter)
        {
            try
            {
                if (string.IsNullOrEmpty(filter.TableName))
                {
                    return BadRequest("表名不能为空");
                }

                byte[] fileBytes = await _temperatureDataService.ExportToExcelAsync(filter);

                // 获取地点显示名称
                List<LocationInfo> locations = await _temperatureDataService.GetAvailableLocationsAsync();
                LocationInfo? location = locations.FirstOrDefault(l => l.TableName.Equals(filter.TableName, StringComparison.OrdinalIgnoreCase));
                string displayName = location?.DisplayName ?? filter.TableName;

                return File(fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"{displayName}_温度数据_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出Excel时发生错误");
                return StatusCode(500, "内部服务器错误");
            }
        }
    }
}