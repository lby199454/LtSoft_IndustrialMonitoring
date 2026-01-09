using System.Text;
using LtSoft_IndustrialMonitoring.Data;
using LtSoft_IndustrialMonitoring.Interfaces;
using LtSoft_IndustrialMonitoring.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace LtSoft_IndustrialMonitoring.Services
{
    /// <summary>
    /// 温度数据服务
    /// </summary>
    public class TemperatureDataService : ITemperatureDataService
    {
        private readonly TemperatureDataContext _context;
        private readonly ILogger<TemperatureDataService> _logger;

        // 预定义的地点信息
        private readonly List<LocationInfo> _locations = new List<LocationInfo>
        {
            new LocationInfo { Name = "qinglong", TableName = "qinglong_tempdata", DisplayName = "青龙" },
            new LocationInfo { Name = "shigao", TableName = "shigao_tempdata", DisplayName = "视高" },
            new LocationInfo { Name = "dongbu", TableName = "dongbu_tempdata", DisplayName = "东部" },
            new LocationInfo { Name = "dazhou", TableName = "dazhou_tempdata", DisplayName = "达州" },
            new LocationInfo { Name = "yibin", TableName = "yibin_tempdata", DisplayName = "宜宾" },
            new LocationInfo { Name = "jintang", TableName = "jintang_tempdata", DisplayName = "金堂" },
            new LocationInfo { Name = "lingshui", TableName = "lingshui_tempdata", DisplayName = "陵水" },
            new LocationInfo { Name = "dingan", TableName = "dingan_tempdata", DisplayName = "定安" }
        };

        public TemperatureDataService(TemperatureDataContext context, ILogger<TemperatureDataService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<LocationInfo>> GetAvailableLocationsAsync()
        {
            return await Task.FromResult(_locations);
        }

        public async Task<TemperatureDataResponse> GetFilteredDataAsync(TemperatureDataFilter filter)
        {
            try
            {
                // 验证表名是否有效
                if (!_locations.Any(l => l.TableName.Equals(filter.TableName, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException($"无效的表名: {filter.TableName}");
                }

                // 构建动态SQL查询
                IQueryable<TemperatureData> query = BuildDynamicQuery(filter);

                // 获取总数
                string countQuery = $"SELECT COUNT(*) FROM {filter.TableName} WHERE {BuildWhereClause(filter)}";
                int totalCount = await _context.Database.ExecuteSqlRawAsync(countQuery);

                // 获取分页数据
                string dataQuery = $@"
                    SELECT id, address_id, date_now, temp_value, logs 
                    FROM {filter.TableName} 
                    WHERE {BuildWhereClause(filter)}
                    ORDER BY date_now DESC
                    LIMIT {filter.PageSize} OFFSET {(filter.PageNumber - 1) * filter.PageSize}";

                List<TemperatureData> data = await _context.TemperatureData
                    .FromSqlRaw(dataQuery)
                    .Select(t => new TemperatureData
                    {
                        Id = t.Id,
                        AddressId = t.AddressId,
                        DateNow = t.DateNow,
                        TempValue = t.TempValue,
                        Logs = t.Logs
                    })
                    .ToListAsync();

                return new TemperatureDataResponse
                {
                    Data = data,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取温度数据时发生错误");
                throw;
            }
        }

        private string BuildWhereClause(TemperatureDataFilter filter)
        {
            List<string> conditions = new List<string>();

            if (filter.AddressId.HasValue)
            {
                conditions.Add($"address_id = {filter.AddressId.Value}");
            }

            if (filter.StartTime.HasValue)
            {
                conditions.Add($"date_now >= '{filter.StartTime.Value:yyyy-MM-dd HH:mm:ss}'");
            }

            if (filter.EndTime.HasValue)
            {
                conditions.Add($"date_now <= '{filter.EndTime.Value:yyyy-MM-dd HH:mm:ss}'");
            }

            return conditions.Any() ? string.Join(" AND ", conditions) : "1=1";
        }

        public async Task<byte[]> ExportToCsvAsync(TemperatureDataFilter filter)
        {
            try
            {
                // 获取所有数据（不分页）
                string allDataQuery = $@"
                    SELECT id, address_id, date_now, temp_value, logs 
                    FROM {filter.TableName} 
                    WHERE {BuildWhereClause(filter)}
                    ORDER BY date_now DESC";

                List<TemperatureData> data = await _context.TemperatureData
                    .FromSqlRaw(allDataQuery)
                    .Select(t => new TemperatureData
                    {
                        Id = t.Id,
                        AddressId = t.AddressId,
                        DateNow = t.DateNow,
                        TempValue = t.TempValue,
                        Logs = t.Logs
                    })
                    .ToListAsync();

                // 生成CSV内容
                StringBuilder csvContent = new StringBuilder();
                csvContent.AppendLine("ID,设备地址ID,记录时间,温度值,日志信息");

                foreach (var item in data)
                {
                    csvContent.AppendLine($"{item.Id},{item.AddressId},{item.DateNow:yyyy-MM-dd HH:mm:ss},{item.TempValue},\"{item.Logs}\"");
                }

                return Encoding.UTF8.GetBytes(csvContent.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出CSV时发生错误");
                throw;
            }
        }

        public async Task<byte[]> ExportToExcelAsync(TemperatureDataFilter filter)
        {
            try
            {
                // 获取所有数据（不分页）
                string allDataQuery = $@"
                    SELECT id, address_id, date_now, temp_value, logs 
                    FROM {filter.TableName} 
                    WHERE {BuildWhereClause(filter)}
                    ORDER BY date_now DESC";

                List<TemperatureData> data = await _context.TemperatureData
                    .FromSqlRaw(allDataQuery)
                    .Select(t => new TemperatureData
                    {
                        Id = t.Id,
                        AddressId = t.AddressId,
                        DateNow = t.DateNow,
                        TempValue = t.TempValue,
                        Logs = t.Logs
                    })
                    .ToListAsync();

                // 获取地点显示名称
                LocationInfo? location = _locations.FirstOrDefault(l => l.TableName.Equals(filter.TableName, StringComparison.OrdinalIgnoreCase));
                string displayName = location?.DisplayName ?? filter.TableName;

                using ExcelPackage package = new ExcelPackage();
                using (ExcelWorksheet worksheet = package.Workbook.Worksheets.Add($"{displayName}温度数据"))
                {

                    // 添加标题行
                    worksheet.Cells[1, 1].Value = "ID";
                    worksheet.Cells[1, 2].Value = "设备地址ID";
                    worksheet.Cells[1, 3].Value = "记录时间";
                    worksheet.Cells[1, 4].Value = "温度值";
                    worksheet.Cells[1, 5].Value = "日志信息";

                    // 设置标题样式
                    using (ExcelRange range = worksheet.Cells[1, 1, 1, 5])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // 添加数据行
                    for (int i = 0; i < data.Count; i++)
                    {
                        TemperatureData item = data[i];
                        worksheet.Cells[i + 2, 1].Value = item.Id;
                        worksheet.Cells[i + 2, 2].Value = item.AddressId;
                        worksheet.Cells[i + 2, 3].Value = item.DateNow;
                        worksheet.Cells[i + 2, 4].Value = item.TempValue;
                        worksheet.Cells[i + 2, 5].Value = item.Logs;
                    }

                    // 设置日期格式
                    using (ExcelRange range = worksheet.Cells[2, 3, data.Count + 1, 3])
                    {
                        range.Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                    }

                    // 自动调整列宽
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                }

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出Excel时发生错误");
                throw;
            }
        }

        // 辅助方法：构建动态查询
        private IQueryable<TemperatureData> BuildDynamicQuery(TemperatureDataFilter filter)
        {
            // 由于我们需要动态表名，这里返回一个空的查询
            // 实际查询在GetFilteredDataAsync中使用原生SQL实现
            return Enumerable.Empty<TemperatureData>().AsQueryable();
        }
    }
}