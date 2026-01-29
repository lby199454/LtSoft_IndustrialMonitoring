using System.Data.Common;
using LtSoft_IndustrialMonitoring.Data;
using LtSoft_IndustrialMonitoring.Interfaces;
using LtSoft_IndustrialMonitoring.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace LtSoft_IndustrialMonitoring.Services
{
	public class ElecMeterDataService : IElecMeterDataService
	{
		private readonly ElecMeterDataContext _context; // use ElecMeterDataContext for electric meter DB
		private readonly ILogger<ElecMeterDataService> _logger;

		private readonly List<LocationInfo> _locations = new List<LocationInfo>
		{
			new LocationInfo { Name = "qinglong", TableName = "qinglong_elecdata", DisplayName = "青龙站" },
			new LocationInfo { Name = "shigao", TableName = "shigao_elecdata", DisplayName = "视高站" },
			new LocationInfo { Name = "dongbu", TableName = "dongbu_elecdata", DisplayName = "东部站" },
			new LocationInfo { Name = "dazhou", TableName = "dazhou_elecdata", DisplayName = "达州站" },
			new LocationInfo { Name = "yibin", TableName = "yibin_elecdata", DisplayName = "宜宾站" },
			new LocationInfo { Name = "jintang", TableName = "jintang_elecdata", DisplayName = "金堂站" },
			new LocationInfo { Name = "lingshui", TableName = "lingshui_elecdata", DisplayName = "陵水站" },
			new LocationInfo { Name = "dingan", TableName = "dingan_elecdata", DisplayName = "定安站" }
		};

		public ElecMeterDataService(ElecMeterDataContext context, ILogger<ElecMeterDataService> logger)
		{
			_context = context;
			_logger = logger;
		}

		/// <summary>
		/// 返回所有可用的站点信息（名称、对应表名、显示名称）
		/// </summary>
		/// <returns></returns>
		public async Task<List<LocationInfo>> GetAvailableLocationsAsync()
		{
			return await Task.FromResult(_locations);
		}

		/// <summary>
		/// 返回每个meter_no的最新一条数据（按collect_time）
		/// </summary>
		/// <param name="tableName"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public async Task<List<ElecMeterData>> GetLatestMetersAsync(string tableName)
		{
			if (!_locations.Any(l => l.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)))
				throw new ArgumentException($"无效的表名: {tableName}");

			using DbConnection conn = _context.Database.GetDbConnection();
			if (conn.State != System.Data.ConnectionState.Open)
				await conn.OpenAsync();

			using DbCommand cmd = conn.CreateCommand();
			cmd.CommandText = $@"
				SELECT t.id, t.meter_no, t.meter_addr, t.meter_name, t.kwh, t.collect_time, t.site_name, t.company_name
				FROM `{tableName}` t
				INNER JOIN (
					SELECT meter_no, MAX(collect_time) AS max_time
					FROM `{tableName}`
					GROUP BY meter_no
				) m ON t.meter_no = m.meter_no AND t.collect_time = m.max_time
				ORDER BY t.meter_no";

			List<ElecMeterData> list = new List<ElecMeterData>();
			using DbDataReader reader = await cmd.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				list.Add(new ElecMeterData
				{
					Id = reader.GetInt64(0),
					MeterNo = reader.GetInt32(1),
					MeterAddr = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
					MeterName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
					Kwh = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
					CollectTime = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5),
					SiteName = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
					CompanyName = reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
				});
			}
			return list;
		}

		/// <summary>
		/// 根据过滤条件（表名、meter_no、时间范围）分页查询数据，并返回总数以供前端分页使用
		/// </summary>
		/// <param name="filter"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public async Task<ElecDataResponse> GetFilteredDataAsync(ElecDataFilter filter)
		{
			if (!_locations.Any(l => l.TableName.Equals(filter.TableName, StringComparison.OrdinalIgnoreCase)))
				throw new ArgumentException($"无效的表名: {filter.TableName}");

			using DbConnection conn = _context.Database.GetDbConnection();
			if (conn.State != System.Data.ConnectionState.Open)
				await conn.OpenAsync();

			// 构建where
			List<string> cond = new();
			if (filter.MeterNo.HasValue) cond.Add($"meter_no = {filter.MeterNo.Value}");
			if (filter.StartTime.HasValue) cond.Add($"collect_time >= '{filter.StartTime.Value:yyyy-MM-dd HH:mm:ss}'");
			if (filter.EndTime.HasValue) cond.Add($"collect_time <= '{filter.EndTime.Value:yyyy-MM-dd HH:mm:ss}'");
			string where = cond.Count > 0 ? string.Join(" AND ", cond) : "1=1";

			// 总数
			using var countCmd = conn.CreateCommand();
			countCmd.CommandText = $"SELECT COUNT(*) FROM `{filter.TableName}` WHERE {where}";
			int total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

			// 数据分页
			using var dataCmd = conn.CreateCommand();
			dataCmd.CommandText = $@"
				SELECT id, meter_no, meter_addr, meter_name, kwh, collect_time, site_name, company_name
				FROM `{filter.TableName}`
				WHERE {where}
				ORDER BY collect_time DESC
				LIMIT @limit OFFSET @offset";
			DbParameter pLimit = dataCmd.CreateParameter(); pLimit.ParameterName = "@limit"; pLimit.Value = filter.PageSize; dataCmd.Parameters.Add(pLimit);
			DbParameter pOffset = dataCmd.CreateParameter(); pOffset.ParameterName = "@offset"; pOffset.Value = (filter.PageNumber - 1) * filter.PageSize; dataCmd.Parameters.Add(pOffset);

			List<ElecMeterData> list = new();
			using var reader = await dataCmd.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				list.Add(new ElecMeterData
				{
					Id = reader.GetInt64(0),
					MeterNo = reader.GetInt32(1),
					MeterAddr = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
					MeterName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
					Kwh = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
					CollectTime = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5),
					SiteName = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
					CompanyName = reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
				});
			}

			return new ElecDataResponse { Data = list, TotalCount = total, PageNumber = filter.PageNumber, PageSize = filter.PageSize };
		}

		/// <summary>
		/// 根据过滤条件查询数据，并生成Excel文件的字节数组返回给前端进行下载
		/// </summary>
		/// <param name="filter"></param>
		/// <returns></returns>
		public async Task<byte[]> ExportToExcelAsync(ElecDataFilter filter)
		{
			// reuse GetFilteredDataAsync to obtain rows, then write to Excel
			ElecDataResponse res = await GetFilteredDataAsync(filter);
			using ExcelPackage package = new ExcelPackage();
			ExcelWorksheet ws = package.Workbook.Worksheets.Add("ElecData");
			ws.Cells[1, 1].Value = "ID";
			ws.Cells[1, 2].Value = "MeterNo";
			ws.Cells[1, 3].Value = "MeterAddr";
			ws.Cells[1, 4].Value = "MeterName";
			ws.Cells[1, 5].Value = "Kwh";
			ws.Cells[1, 6].Value = "CollectTime";
			ws.Cells[1, 7].Value = "SiteName";
			ws.Cells[1, 8].Value = "CompanyName";

			for (int i = 0; i < res.Data.Count; i++)
			{
				var r = res.Data[i];
				ws.Cells[i + 2, 1].Value = r.Id;
				ws.Cells[i + 2, 2].Value = r.MeterNo;
				ws.Cells[i + 2, 3].Value = r.MeterAddr;
				ws.Cells[i + 2, 4].Value = r.MeterName;
				ws.Cells[i + 2, 5].Value = r.Kwh;
				ws.Cells[i + 2, 6].Value = r.CollectTime;
				ws.Cells[i + 2, 7].Value = r.SiteName;
				ws.Cells[i + 2, 8].Value = r.CompanyName;
			}
			ws.Cells[ws.Dimension.Address].AutoFitColumns();
			return package.GetAsByteArray();
		}
	}
}
