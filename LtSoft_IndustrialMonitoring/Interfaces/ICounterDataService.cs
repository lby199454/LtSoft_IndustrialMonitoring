using LtSoft_IndustrialMonitoring.Models;

namespace LtSoft_IndustrialMonitoring.Interfaces
{
    /// <summary>
    /// 筛分计数数据服务接口
    /// </summary>
    public interface ICounterDataService
    {
        Task<List<LocationInfo>> GetAvailableLocationsAsync();                    // 获取可用地点列表
        Task<CounterDataResponse> GetFilteredDataAsync(CounterDataFilter filter); // 获取筛选后的数据
        Task<CounterSummary> GetCounterSummaryAsync(CounterDataFilter filter);    // 获取计数器摘要
        Task<CounterDayStats> GetHistoricalTotalsAsync(string tableName);         // 获取历史总量
        Task<CounterDayStats> GetYesterdayStatsAsync(string tableName);           // 获取昨日统计
        Task<CounterDayStats> GetTodayStatsAsync(string tableName);               // 获取今日统计
        Task<byte[]> ExportToCsvAsync(CounterDataFilter filter);                  // 导出为CSV
        Task<byte[]> ExportToExcelAsync(CounterDataFilter filter);                // 导出为Excel
        Task<byte[]> ExportAllSitesStatsExcelAsync(DateTime start, DateTime end); // 导出所有站点在指定时间范围内为Excel
    }
}
