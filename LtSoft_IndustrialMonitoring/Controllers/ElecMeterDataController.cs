using LtSoft_IndustrialMonitoring.Interfaces;
using LtSoft_IndustrialMonitoring.Models;
using Microsoft.AspNetCore.Mvc;

namespace LtSoft_IndustrialMonitoring.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ElecMeterDataController : ControllerBase
	{
		private readonly IElecMeterDataService _service;
		private readonly ILogger<ElecMeterDataController> _logger;

		public ElecMeterDataController(IElecMeterDataService service, ILogger<ElecMeterDataController> logger)
		{
			_service = service;
			_logger = logger;
		}

		/// <summary>
		/// 获取站点列表
		/// 请求: GET /api/elecmeterdata/locations
		/// </summary>
		/// <returns>
		/// 响应: 200 [ { "name":"qinglong", "tableName":"qinglong_elecdata", "displayName":"青龙站" }, ... ]
		/// </returns>
		[HttpGet("locations")]
		public async Task<ActionResult<List<LocationInfo>>> GetLocations()
		{
			return Ok(await _service.GetAvailableLocationsAsync());
		}

		/// <summary>
		/// 获取每个 meter_no 的最新记录
		/// 请求: GET /api/elecmeterdata/{tableName}/latest
		/// </summary>
		/// <param name="tableName"></param>
		/// <returns>
		/// 响应: 200 [ { "id": 123, "meterNo": 1, "meterAddr": "0001", "meterName": "电表A", "kwh": 123.456, "collectTime": "2026-01-14T17:38:00", "siteName": "青龙站", "companyName": "公司A" }, ... ]
		/// </returns>
		[HttpGet("{tableName}/latest")]
		public async Task<ActionResult<List<ElecMeterData>>> GetLatestMeters(string tableName)
		{
			try
			{
				List<ElecMeterData> data = await _service.GetLatestMetersAsync(tableName);
				return Ok(data);
			}
			catch (ArgumentException ex)
			{
				return BadRequest(ex.Message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetLatestMeters error");
				return StatusCode(500, "内部服务器错误");
			}
		}

		/// <summary>
		/// 分页查询（原始数据）
		/// 请求: POST /api/elecmeterdata/filter
		/// </summary>
		/// <param name="filter"></param>
		/// <returns>
		/// 响应: 200 { "data": [ ...rows... ], "totalCount": 345, "pageNumber": 1, "pageSize": 100 }
		/// </returns>
		[HttpPost("filter")]
		public async Task<ActionResult<ElecDataResponse>> GetFiltered([FromBody] ElecDataFilter filter)
		{
			try
			{
				return Ok(await _service.GetFilteredDataAsync(filter));
			}
			catch (ArgumentException ex)
			{
				return BadRequest(ex.Message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetFiltered error");
				return StatusCode(500, "内部服务器错误");
			}
		}

		/// <summary>
		/// 导出为 Excel
		/// 请求: POST /api/elecmeterdata/export/excel
		/// </summary>
		/// <param name="filter"></param>
		/// <returns>
		/// 响应: 200 返回 Excel 文件
		/// </returns>
		[HttpPost("export/excel")]
		public async Task<IActionResult> ExportExcel([FromBody] ElecDataFilter filter)
		{
			try
			{
				byte[] bytes = await _service.ExportToExcelAsync(filter);
				string fileName = $"ElecData_{filter.TableName}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
				return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
			}
			catch (ArgumentException ex)
			{
				return BadRequest(ex.Message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "ExportExcel error");
				return StatusCode(500, "内部服务器错误");
			}
		}
	}
}
