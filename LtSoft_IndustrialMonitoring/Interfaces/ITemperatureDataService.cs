using LtSoft_IndustrialMonitoring.Models;

namespace LtSoft_IndustrialMonitoring.Interfaces
{
    /// <summary>
    /// 温度数据服务接口
    /// </summary>
    public interface ITemperatureDataService
    {
        Task<List<LocationInfo>> GetAvailableLocationsAsync();
        Task<TemperatureDataResponse> GetFilteredDataAsync(TemperatureDataFilter filter);
        Task<byte[]> ExportToCsvAsync(TemperatureDataFilter filter);
        Task<byte[]> ExportToExcelAsync(TemperatureDataFilter filter);
    }
}
