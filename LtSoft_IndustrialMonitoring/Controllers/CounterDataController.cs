using LtSoft_IndustrialMonitoring.Interfaces;
using LtSoft_IndustrialMonitoring.Models;

using Microsoft.AspNetCore.Mvc;

namespace LtSoft_IndustrialMonitoring.Controllers
{
    /// <summary>
    /// 筛分计数控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CounterDataController : ControllerBase
    {
        private readonly ICounterDataService _counterDataService;
        private readonly ILogger<CounterDataController> _logger;

        public CounterDataController(ICounterDataService counterDataService, ILogger<CounterDataController> logger)
        {
            _counterDataService = counterDataService;
            _logger = logger;
        }

        /// <summary>
        /// 获取可用的计数器地点列表
        /// 请求示例:
        /// GET /api/counterdata/locations
        /// 响应示例:
        /// [
        ///     {
        ///         "Name": "Counter_1",
        ///         "TableName": "Counter_1",
        ///         "DisplayName": "计数器1"
        ///     },
        ///     {
        ///         "Name": "Counter_2",
        ///         "TableName": "Counter_2",
        ///         "DisplayName": "计数器2"
        ///     }
        /// ]
        /// </summary>
        /// <returns></returns>
        [HttpGet("locations")]
        public async Task<ActionResult<List<LocationInfo>>> GetAvailableLocations()
        {
            try
            {
                List<LocationInfo> locations = await _counterDataService.GetAvailableLocationsAsync();
                return Ok(locations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取计数器地点列表时发生错误");
                return StatusCode(500, "内部服务器错误");
            }
        }

        /// <summary>
        /// 获取筛选后的计数器数据
        /// 请求示例:
        /// {
        ///     "TableName": "Counter_1",
        ///     "DeviceId": 1,
        ///     "StartTime": "2023-07-01T00:00:00",
        ///     "EndTime": "2023-07-01T23:59:59",
        ///     "PageNumber": 1,
        ///     "PageSize": 100
        /// }
        /// 响应示例:
        /// {
        ///     "Data": [
        ///         {
        ///             "Id": 1,
        ///             "DeviceId": 1,
        ///             "DateNow": "2023-07-01T00:00:00",
        ///             "Count": 100,
        ///             "Logs": "日志信息"
        ///         },
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpPost("filter")]
        public async Task<ActionResult<CounterDataResponse>> GetFilteredData([FromBody] CounterDataFilter filter)
        {
            try
            {
                if (string.IsNullOrEmpty(filter.TableName))
                    return BadRequest("表名不能为空");

                CounterDataResponse result = await _counterDataService.GetFilteredDataAsync(filter);
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

        /// <summary>
        /// 获取计数器摘要信息
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpPost("summary")]
        public async Task<ActionResult<CounterSummary>> GetCounterSummary([FromBody] CounterDataFilter filter)
        {
            try
            {
                if (string.IsNullOrEmpty(filter.TableName))
                    return BadRequest("表名不能为空");

                CounterSummary result = await _counterDataService.GetCounterSummaryAsync(filter);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取计数器摘要时发生错误");
                return StatusCode(500, "内部服务器错误");
            }
        }

        /// <summary>
        /// 历史总量
        /// 请求示例:
        /// GET /api/counterdata/Counter_1/historical?start=2023-07-01T00:00:00&end=2023-07-01T23:59:59
        /// 响应示例:
        /// {
        ///     "TotalCount": 100,
        ///     "TotalRecords": 100,
        ///     "Start": "2023-07-01T00:00:00",
        ///     "End": "2023-07-01T23:59:59"
        /// }
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        [HttpGet("{tableName}/historical")]
        public async Task<ActionResult<CounterDayStats>> GetHistoricalTotals(string tableName)
        {
            try
            {
                CounterDayStats result = await _counterDataService.GetHistoricalTotalsAsync(tableName);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "1、【History】获取历史总量时发生错误");
                return StatusCode(500, "内部服务器错误");
            }
        }

        /// <summary>
        /// 昨日统计（00:00-24:00）
        /// 请求示例:
        /// GET /api/counterdata/Counter_1/historical?start=2023-07-01T00:00:00&end=2023-07-01T23:59:59
        /// 响应示例:
        /// {
        ///     "TotalCount": 100,
        ///     "TotalRecords": 100,
        ///     "Start": "2023-07-01T00:00:00",
        ///     "End": "2023-07-01T23:59:59"
        /// }
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        [HttpGet("{tableName}/yesterday")]
        public async Task<ActionResult<CounterDayStats>> GetYesterdayStats(string tableName)
        {
            try
            {
                CounterDayStats result = await _counterDataService.GetYesterdayStatsAsync(tableName);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "2、【Yesterday】获取昨日统计时发生错误");
                return StatusCode(500, "内部服务器错误");
            }
        }

        /// <summary>
        /// 今日统计（00:00 - now）前端刷新时调用
        /// 请求示例:
        /// GET /api/counterdata/Counter_1/today
        /// 响应示例:
        /// {
        ///     "TotalCount": 100,
        ///     "TotalRecords": 100,
        ///     "Start": "2023-07-01T00:00:00",
        ///     "End": "2023-07-01T23:59:59"
        /// }
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        [HttpGet("{tableName}/today")]
        public async Task<ActionResult<CounterDayStats>> GetTodayStats(string tableName)
        {
            try
            {
                CounterDayStats result = await _counterDataService.GetTodayStatsAsync(tableName);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "3、【Today】获取今日统计时发生错误");
                return StatusCode(500, "内部服务器错误");
            }
        }

        /// <summary>
        /// 导出筛选后的计数器数据为CSV文件
        /// 请求示例:
        /// POST /api/counterdata/export/csv:
        /// {
        ///     "TableName": "Counter_1",
        ///     "DeviceId": 1,
        ///     "StartTime": "2023-07-01T00:00:00",
        ///     "EndTime": "2023-07-01T23:59:59",
        ///     "PageNumber": 1,
        ///     "PageSize": 100
        /// }
        /// 响应示例:
        /// {
        ///     "TableName": "Counter_1",
        ///     "DeviceId": 1,
        ///     "StartTime": "2023-07-01T00:00:00",
        ///     "EndTime": "2023-07-01T23:59:59",
        ///     "PageNumber": 1,
        ///     "PageSize": 100
        /// }
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpPost("export/csv")]
        public async Task<IActionResult> ExportToCsv([FromBody] CounterDataFilter filter)
        {
            try
            {
                if (string.IsNullOrEmpty(filter.TableName))
                {
                    return BadRequest("表名不能为空");
                }

                byte[] fileBytes = await _counterDataService.ExportToCsvAsync(filter);

                // 获取地点显示名称
                List<LocationInfo> locations = await _counterDataService.GetAvailableLocationsAsync();
                LocationInfo? location = locations.FirstOrDefault(l => l.TableName.Equals(filter.TableName, StringComparison.OrdinalIgnoreCase));
                string displayName = location?.DisplayName ?? filter.TableName;

                return File(fileBytes,
                    "text/csv",
                    $"{displayName}_计数器数据_{DateTime.Now:yyyyMMddHHmmss}.csv");
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

        /// <summary>
        /// 导出筛选后的计数器数据为Excel文件
        /// 请求示例:
        /// POST /api/counterdata/export/excel
        /// {
        ///     "tableName": "Counter_01",
        ///     "deviceId": 1,
        ///     "startTime": "2023-07-01T00:00:00",
        ///     "endTime": "2023-07-31T23:59:59",
        ///     "pageNumber": 1,
        ///     "pageSize": 100
        /// }
        /// 响应示例:
        /// {
        ///     "TableName": "Counter_01",
        ///     "DeviceId": 1,
        ///     "StartTime": "2023-07-01T00:00:00",
        ///     "EndTime": "2023-07-31T23:59:59",
        ///     "PageNumber": 1,
        ///     "PageSize": 100
        /// }
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpPost("export/excel")]
        public async Task<IActionResult> ExportToExcel([FromBody] CounterDataFilter filter)
        {
            try
            {
                if (string.IsNullOrEmpty(filter.TableName))
                {
                    return BadRequest("表名不能为空");
                }

                byte[] fileBytes = await _counterDataService.ExportToExcelAsync(filter);

                // 获取地点显示名称
                List<LocationInfo> locations = await _counterDataService.GetAvailableLocationsAsync();
                LocationInfo? location = locations.FirstOrDefault(l => l.TableName.Equals(filter.TableName, StringComparison.OrdinalIgnoreCase));
                string displayName = location?.DisplayName ?? filter.TableName;

                return File(fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"{displayName}_计数器数据_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
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

        /// <summary>
        /// 导出所有站点的统计数据为Excel文件
        /// 请求示例: POST /api/counterdata/export/allsites
        /// {
        ///     "start": "2026-01-02T02:00:00",
        ///     "end": "2026-01-05T07:00:00"
        /// }
        /// 响应示例:
        /// {
        ///     "FileName": "AllSites_Stats_202601020200_202601050700.xlsx",
        ///     "FileSize": 12345
        /// }
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("export/allsites")]
        public async Task<IActionResult> ExportAllSitesStats([FromBody] ExportAllSitesRequest request)
        {
            try
            {
                if (request.End <= request.Start)
                    return BadRequest("结束时间必须大于开始时间");

                byte[] fileBytes = await _counterDataService.ExportAllSitesStatsExcelAsync(request.Start, request.End);
                string fileName = $"AllSites_Stats_{request.Start:yyyyMMddHHmm}_{request.End:yyyyMMddHHmm}.xlsx";
                return File(fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出所有站点统计时出错");
                return StatusCode(500, "内部服务器错误");
            }
        }
    }
}