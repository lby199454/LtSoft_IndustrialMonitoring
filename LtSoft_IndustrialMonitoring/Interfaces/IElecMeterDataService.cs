using LtSoft_IndustrialMonitoring.Models;

namespace LtSoft_IndustrialMonitoring.Interfaces
{
	public interface IElecMeterDataService
	{
		Task<List<LocationInfo>> GetAvailableLocationsAsync();
		Task<List<ElecMeterData>> GetLatestMetersAsync(string tableName);
		Task<ElecDataResponse> GetFilteredDataAsync(ElecDataFilter filter);
		Task<byte[]> ExportToExcelAsync(ElecDataFilter filter);
	}
}
