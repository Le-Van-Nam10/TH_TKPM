using System.Linq;
using System.Threading.Tasks;
using ASC.Business.Interfaces;
using ASC.WEB.Data;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace ASC.Web.Data
{
    public class MasterDataCacheOperations : IMasterDataCacheOperations
    {
        private readonly IDistributedCache _cache;
        private readonly IMasterDataOperations _masterData;

        // ✅ Dùng tên key đơn giản — Redis sẽ tự thêm prefix "ASCInstance" nếu đã cấu hình InstanceName
        private readonly string MasterDataCacheName = "MasterDataCache";

        public MasterDataCacheOperations(IDistributedCache cache, IMasterDataOperations masterData)
        {
            _cache = cache;
            _masterData = masterData;
        }

        public async Task CreateMasterDataCacheAsync()
        {
            var keys = (await _masterData.GetAllMasterKeysAsync())
                        .Where(p => p.IsActive).ToList();

            var values = (await _masterData.GetAllMasterValuesAsync())
                        .Where(p => p.IsActive).ToList();

            var masterDataCache = new MasterDataCache
            {
                Keys = keys,
                Values = values
            };

            var json = JsonConvert.SerializeObject(masterDataCache);

            await _cache.SetStringAsync(MasterDataCacheName, json);
        }

        public async Task<MasterDataCache> GetMasterDataCacheAsync()
        {
            var data = await _cache.GetStringAsync(MasterDataCacheName);
            if (data == null) return null;
            return JsonConvert.DeserializeObject<MasterDataCache>(data);
        }
    }
}
