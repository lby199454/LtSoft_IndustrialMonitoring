using System.Data;
using System.Data.Common;
using System.Linq.Dynamic.Core;
using System.Text;
using LtSoft_IndustrialMonitoring.Data;
using LtSoft_IndustrialMonitoring.Interfaces;
using LtSoft_IndustrialMonitoring.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace LtSoft_IndustrialMonitoring.Services
{
    /// <summary>
    /// 筛分计数数据服务
    /// </summary>
    public class CounterDataService : ICounterDataService
    {
        private readonly CounterDataContext _context;
        private readonly ILogger<CounterDataService> _logger;

        // 筛分计数表
        private readonly List<LocationInfo> _locations = new List<LocationInfo>
        {
            new LocationInfo { Name = "qinglong", TableName = "qinglong_counter", DisplayName = "青龙站" },
            new LocationInfo { Name = "shigao", TableName = "shigao_counter", DisplayName = "视高站" },
            new LocationInfo { Name = "dongbu", TableName = "dongbu_counter", DisplayName = "东部站" },
            new LocationInfo { Name = "dazhou", TableName = "dazhou_counter", DisplayName = "达州站" },
            new LocationInfo { Name = "yibin", TableName = "yibin_counter", DisplayName = "宜宾站" },
            new LocationInfo { Name = "jintang", TableName = "jintang_counter", DisplayName = "金堂站" },
            new LocationInfo { Name = "lingshui", TableName = "lingshui_counter", DisplayName = "陵水站" },
            new LocationInfo { Name = "dingan", TableName = "dingan_counter", DisplayName = "定安站" }
        };

        /// <summary>
        /// 添加日期参数到命令
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        private static void AddDateParam(DbCommand cmd, string name, DateTime value)
        {
            DbParameter p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }

        /// <summary>
        /// 辅助类用于SUM查询结果
        /// </summary>
        private class SumResult
        {
            public int Value { get; set; }
        }

        /// <summary>
        /// 辅助类用于时间范围查询
        /// </summary>
        private class TimeRangeResult
        {
            public DateTime? FirstTime { get; set; }
            public DateTime? LastTime { get; set; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        public CounterDataService(CounterDataContext context, ILogger<CounterDataService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 获取可用地点列表
        /// </summary>
        /// <returns></returns>
        public async Task<List<LocationInfo>> GetAvailableLocationsAsync()
        {
            return await Task.FromResult(_locations);
        }

        /// <summary>
        /// 根据筛选条件获取计数器数据
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<CounterDataResponse> GetFilteredDataAsync(CounterDataFilter filter)
        {
            try
            {
                LocationInfo? location = _locations.FirstOrDefault(l => l.TableName.Equals(filter.TableName, StringComparison.OrdinalIgnoreCase))
                    ?? throw new ArgumentException($"无效的表名: {filter.TableName}");
                string whereClause = BuildWhereClause(filter);

                using DbConnection connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                // 1️、COUNT(*)
                using DbCommand countCmd = connection.CreateCommand();
                countCmd.CommandText = $@"
                    SELECT COUNT(*) 
                    FROM `{filter.TableName}`
                    WHERE {whereClause}";
                int totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

                // 2、SUM(count)
                using DbCommand sumCmd = connection.CreateCommand();
                sumCmd.CommandText = $@"
                    SELECT COALESCE(SUM(`count`), 0)
                    FROM `{filter.TableName}`
                    WHERE {whereClause}";
                int sumCount = Convert.ToInt32(await sumCmd.ExecuteScalarAsync());

                // 3️、分页数据
                using DbCommand dataCmd = connection.CreateCommand();
                dataCmd.CommandText = $@"
                    SELECT id, device_id, date_now, `count`, logs, reserved1, reserved2, reserved3
                    FROM `{filter.TableName}`
                    WHERE {whereClause}
                    ORDER BY date_now DESC
                    LIMIT @limit OFFSET @offset";

                DbParameter pLimit = dataCmd.CreateParameter();
                pLimit.ParameterName = "@limit";
                pLimit.Value = filter.PageSize;
                dataCmd.Parameters.Add(pLimit);

                DbParameter pOffset = dataCmd.CreateParameter();
                pOffset.ParameterName = "@offset";
                pOffset.Value = (filter.PageNumber - 1) * filter.PageSize;
                dataCmd.Parameters.Add(pOffset);

                List<CounterData> data = new List<CounterData>();
                using DbDataReader reader = await dataCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    data.Add(new CounterData
                    {
                        Id = reader.GetInt32("id"),
                        DeviceId = reader.GetInt32("device_id"),
                        DateNow = reader.GetDateTime("date_now"),
                        Count = reader.GetInt32("count"),
                        Logs = (string)reader["logs"]
                    });
                }

                return new CounterDataResponse
                {
                    Data = data,
                    TotalCount = totalCount,
                    SumCount = sumCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取计数器数据时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 获取计数器摘要信息
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<CounterSummary> GetCounterSummaryAsync(CounterDataFilter filter)
        {
            try
            {
                // 验证表名是否有效
                if (!_locations.Any(l => l.TableName.Equals(filter.TableName, StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentException($"无效的表名: {filter.TableName}");
                // 获取总记录数
                string whereClause = BuildWhereClause(filter);
                string countQuery = $"SELECT COUNT(*) FROM {filter.TableName} WHERE {whereClause}";
                int totalRecords = await _context.Database.ExecuteSqlRawAsync(countQuery);

                // 获取count字段累加值
                //string sumQuery = $"SELECT SUM(count) FROM {filter.TableName} WHERE {whereClause}";
                //int totalCount = await GetSumCountAsync(sumQuery);
                int totalCount = await GetSumCountAsync(filter.TableName, whereClause);

                // 获取时间范围
                string timeRangeQuery = $@"
                    SELECT MIN(date_now) as FirstTime, MAX(date_now) as LastTime 
                    FROM {filter.TableName} 
                    WHERE {whereClause}";

                TimeRangeResult? timeRange = await _context.Database.SqlQueryRaw<TimeRangeResult>($@"
                    SELECT MIN(date_now) as FirstTime, MAX(date_now) as LastTime 
                    FROM {filter.TableName} 
                    WHERE {whereClause}")
                    .FirstOrDefaultAsync();

                // 获取地点显示名称
                LocationInfo? location = _locations.FirstOrDefault(l => l.TableName.Equals(filter.TableName, StringComparison.OrdinalIgnoreCase));

                return new CounterSummary
                {
                    LocationName = location?.DisplayName ?? filter.TableName,
                    TotalRecords = totalRecords,
                    TotalCount = totalCount,
                    FirstRecordTime = timeRange?.FirstTime,
                    LastRecordTime = timeRange?.LastTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取计数器摘要时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 构建WHERE子句
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private static string BuildWhereClause(CounterDataFilter filter)
        {
            List<string> conditions = new List<string>();

            if (filter.DeviceId.HasValue)
            {
                conditions.Add($"device_id = {filter.DeviceId.Value}");
            }

            if (filter.StartTime.HasValue)
            {
                conditions.Add($"date_now >= '{filter.StartTime.Value:yyyy-MM-dd HH:mm:ss}'");
            }

            if (filter.EndTime.HasValue)
            {
                conditions.Add($"date_now <= '{filter.EndTime.Value:yyyy-MM-dd HH:mm:ss}'");
            }

            return conditions.Count != 0 ? string.Join(" AND ", conditions) : "1=1";
        }

        /// <summary>
        /// 获取count字段的累加值
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="whereClause"></param>
        /// <returns></returns>
        private async Task<int> GetSumCountAsync(string tableName, string whereClause)
        {
            try
            {
                string sumQuery =
                    $@"
                    SELECT COALESCE(SUM(`count`), 0) AS Value 
                    FROM {tableName} 
                    WHERE {whereClause}";

                SumResult? result = await _context.Database.SqlQuery<SumResult>(
                    $@"
                    SELECT COALESCE(SUM(`count`), 0) AS Value 
                    FROM {tableName} 
                    WHERE {whereClause}")
                    .FirstOrDefaultAsync();

                return result?.Value ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算count累加值时发生错误");
                return 0;
            }
        }

        /// <summary>
        /// 历史总量
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<CounterDayStats> GetHistoricalTotalsAsync(string tableName)
        {
            // 验证表名
            LocationInfo? location = _locations.FirstOrDefault(l => l.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"无效的表名: {tableName}");
            using DbConnection connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            // 查询总数
            using DbCommand countCommand = connection.CreateCommand();
            countCommand.CommandText = $"SELECT COUNT(*) FROM `{tableName}` WHERE 1=1";

            // 查询总计数 - 使用实际的列名 "count"
            using DbCommand sumCommand = connection.CreateCommand();
            sumCommand.CommandText = $"SELECT COALESCE(SUM(`count`), 0) FROM `{tableName}` WHERE 1=1";

            // 查询时间范围
            using DbCommand timeCommand = connection.CreateCommand();
            timeCommand.CommandText = $"SELECT MIN(date_now), MAX(date_now) FROM `{tableName}` WHERE 1=1";
            using DbDataReader reader = await timeCommand.ExecuteReaderAsync();

            DateTime? firstTime = null;
            DateTime? lastTime = null;

            if (await reader.ReadAsync())
            {
                firstTime = !reader.IsDBNull(0) ? reader.GetDateTime(0) : null;
                lastTime = !reader.IsDBNull(1) ? reader.GetDateTime(1) : null;
            }
            await reader.CloseAsync();

            int totalCount = Convert.ToInt32(await sumCommand.ExecuteScalarAsync());
            int totalRecords = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
            return new CounterDayStats
            {
                TableName = location.DisplayName ?? tableName,
                Start = firstTime ?? DateTime.MinValue,
                End = lastTime ?? DateTime.MinValue,
                TotalRecords = totalRecords,
                TotalCount = totalCount
            };
        }

        /// <summary>
        /// 昨日统计（00:00 - 24:00）
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<CounterDayStats> GetYesterdayStatsAsync(string tableName)
        {
            // 校验表名
            LocationInfo? location = _locations.FirstOrDefault(l => l.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"无效的表名: {tableName}");
            DateTime today = DateTime.Now.Date;
            DateTime yesterdayStart = today.AddDays(-1);
            DateTime yesterdayEnd = today.AddTicks(-1);

            using DbConnection connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            // COUNT(*)
            using DbCommand countCmd = connection.CreateCommand();
            countCmd.CommandText = $@"SELECT COUNT(*) FROM `{tableName}` WHERE date_now >= @start AND date_now <= @end";
            AddDateParam(countCmd, "@start", yesterdayStart);
            AddDateParam(countCmd, "@end", yesterdayEnd);

            // SUM(count)
            using DbCommand sumCmd = connection.CreateCommand();
            sumCmd.CommandText = $@"SELECT COALESCE(SUM(`count`), 0) FROM `{tableName}` WHERE date_now >= @start AND date_now <= @end";
            AddDateParam(sumCmd, "@start", yesterdayStart);
            AddDateParam(sumCmd, "@end", yesterdayEnd);

            int totalRecords = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            int totalCount = Convert.ToInt32(await sumCmd.ExecuteScalarAsync());

            return new CounterDayStats
            {
                TableName = location.DisplayName ?? tableName,
                Start = yesterdayStart,
                End = yesterdayEnd,
                TotalRecords = totalRecords,
                TotalCount = totalCount
            };
        }

        /// <summary>
        /// 今日统计（00:00 - NOW）
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<CounterDayStats> GetTodayStatsAsync(string tableName)
        {
            LocationInfo? location = _locations.FirstOrDefault(l => l.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"无效的表名: {tableName}");
            DateTime todayStart = DateTime.Now.Date;
            DateTime now = DateTime.Now;

            using DbConnection connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using DbCommand countCmd = connection.CreateCommand();
            countCmd.CommandText = $@"SELECT COUNT(*) FROM `{tableName}` WHERE date_now >= @start AND date_now <= @end";
            AddDateParam(countCmd, "@start", todayStart);
            AddDateParam(countCmd, "@end", now);

            using DbCommand sumCmd = connection.CreateCommand();
            sumCmd.CommandText = $@"SELECT COALESCE(SUM(`count`), 0) FROM `{tableName}` WHERE date_now >= @start AND date_now <= @end";
            AddDateParam(sumCmd, "@start", todayStart);
            AddDateParam(sumCmd, "@end", now);

            int totalRecords = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            int totalCount = Convert.ToInt32(await sumCmd.ExecuteScalarAsync());

            return new CounterDayStats
            {
                TableName = location.DisplayName ?? tableName,
                Start = todayStart,
                End = now,
                TotalRecords = totalRecords,
                TotalCount = totalCount
            };
        }

        /// <summary>
        /// 导出数据为CSV格式
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<byte[]> ExportToCsvAsync(CounterDataFilter filter)
        {
            try
            {
                // 获取所有数据（不分页）
                string allDataQuery =
                    $@"
                    SELECT id, device_id, date_now, count, logs, reserved1, reserved2, reserved3 
                    FROM {filter.TableName} 
                    WHERE {BuildWhereClause(filter)}
                    ORDER BY date_now DESC";

                List<CounterData> data = await _context.CounterData
                    .FromSqlRaw(allDataQuery)
                    .Select(t => new CounterData
                    {
                        Id = t.Id,
                        DeviceId = t.DeviceId,
                        DateNow = t.DateNow,
                        Count = t.Count,
                        Logs = t.Logs,
                        Reserved1 = t.Reserved1,
                        Reserved2 = t.Reserved2,
                        Reserved3 = t.Reserved3
                    })
                    .ToListAsync();

                // 生成CSV内容
                StringBuilder csvContent = new StringBuilder();
                csvContent.AppendLine("ID,设备ID,记录时间,计数值,日志信息,预留字段1,预留字段2,预留字段3");

                foreach (CounterData item in data)
                {
                    csvContent.AppendLine($"{item.Id},{item.DeviceId},{item.DateNow:yyyy-MM-dd HH:mm:ss},{item.Count},\"{item.Logs}\",\"{item.Reserved1}\",\"{item.Reserved2}\",\"{item.Reserved3}\"");
                }

                return Encoding.UTF8.GetBytes(csvContent.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出CSV时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 导出数据为Excel格式
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        /// 
        public async Task<byte[]> ExportToExcelAsync(CounterDataFilter filter)
        {
            try
            {
                // 获取所有数据（不分页）
                string allDataQuery =
                    $@"
                    SELECT id, device_id, date_now, count, logs, reserved1, reserved2, reserved3 
                        FROM {filter.TableName} 
                        WHERE {BuildWhereClause(filter)}
                        ORDER BY date_now DESC";

                List<CounterData> data = await _context.CounterData
                    .FromSqlRaw(allDataQuery)
                    .Select(t => new CounterData
                    {
                        Id = t.Id,
                        DeviceId = t.DeviceId,
                        DateNow = t.DateNow,
                        Count = t.Count,
                        Logs = t.Logs,
                        Reserved1 = t.Reserved1,
                        Reserved2 = t.Reserved2,
                        Reserved3 = t.Reserved3
                    })
                    .ToListAsync();

                // 获取地点显示名称
                LocationInfo? location = _locations.FirstOrDefault(l => l.TableName.Equals(filter.TableName, StringComparison.OrdinalIgnoreCase));
                string displayName = location?.DisplayName ?? filter.TableName;

                using ExcelPackage package = new ExcelPackage();
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add($"{displayName}计数器数据");

                // 添加标题行
                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "设备ID";
                worksheet.Cells[1, 3].Value = "记录时间";
                worksheet.Cells[1, 4].Value = "计数值";
                worksheet.Cells[1, 5].Value = "日志信息";
                worksheet.Cells[1, 6].Value = "预留字段1";
                worksheet.Cells[1, 7].Value = "预留字段2";
                worksheet.Cells[1, 8].Value = "预留字段3";

                // 设置标题样式
                using (ExcelRange range = worksheet.Cells[1, 1, 1, 8])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // 添加数据行
                for (int i = 0; i < data.Count; i++)
                {
                    CounterData item = data[i];
                    worksheet.Cells[i + 2, 1].Value = item.Id;
                    worksheet.Cells[i + 2, 2].Value = item.DeviceId;
                    worksheet.Cells[i + 2, 3].Value = item.DateNow;
                    worksheet.Cells[i + 2, 4].Value = item.Count;
                    worksheet.Cells[i + 2, 5].Value = item.Logs;
                    worksheet.Cells[i + 2, 6].Value = item.Reserved1;
                    worksheet.Cells[i + 2, 7].Value = item.Reserved2;
                    worksheet.Cells[i + 2, 8].Value = item.Reserved3;
                }

                // 设置日期格式
                using (ExcelRange range = worksheet.Cells[2, 3, data.Count + 1, 3])
                {
                    range.Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                }

                // 自动调整列宽
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出Excel时发生错误");
                throw;
            }
        }

		/// <summary>
		/// 导出所有站点的统计数据为Excel文件
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		public async Task<byte[]> ExportAllSitesStatsExcelAsync(DateTime start, DateTime end)
        {
            try
            {
                using DbConnection connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                // 准备结果集合
                List<(string Site, int TotalCount)> rows = new List<(string Site, int TotalCount)>();

                foreach (LocationInfo loc in _locations)
                {
                    using DbCommand cmd = connection.CreateCommand();
                    cmd.CommandText = $@"SELECT COALESCE(SUM(`count`),0) FROM `{loc.TableName}` WHERE date_now >= @start AND date_now <= @end";
                    AddDateParam(cmd, "@start", start);
                    AddDateParam(cmd, "@end", end);

                    object val = await cmd.ExecuteScalarAsync();
                    int total = Convert.ToInt32(val);
                    rows.Add((loc.DisplayName ?? loc.TableName, total));
                }

                // 生成Excel
                using ExcelPackage package = new ExcelPackage();
                ExcelWorksheet ws = package.Workbook.Worksheets.Add("AllSitesStats");

                // 标题
                ws.Cells[1, 1].Value = "站点";
                ws.Cells[1, 2].Value = "统计开始";
                ws.Cells[1, 3].Value = "统计结束";
                ws.Cells[1, 4].Value = "总计数";

                // 标题样式
                using (ExcelRange titleRange = ws.Cells[1, 1, 1, 4])
                {
                    titleRange.Style.Font.Bold = true;
                    titleRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    titleRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // 数据行
                for (int i = 0; i < rows.Count; i++)
                {
                    ws.Cells[i + 2, 1].Value = rows[i].Site;
                    ws.Cells[i + 2, 2].Value = start;
                    ws.Cells[i + 2, 3].Value = end;
                    ws.Cells[i + 2, 4].Value = rows[i].TotalCount;
                }

                // 列宽和格式
                ws.Column(2).Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                ws.Column(3).Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出所有站点统计到Excel时出错");
                throw;
            }
        }
    }
}