using ASC.Business.Interfaces;
using ASC.Model.Models;
using ASC.Utilities;
using ASC.WEB.Areas.Configuration.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace ASC.WEB.Areas.Configuration.Controllers
{
    [Area("Configuration")]
    [Authorize(Roles = "Admin")]
    public class MasterDataController : Controller
    {
        private readonly IMasterDataOperations _masterData;
        private readonly IMapper _mapper;
        private readonly ILogger<MasterDataController> _logger;

        public MasterDataController(IMasterDataOperations masterData, IMapper mapper, ILogger<MasterDataController> logger)
        {
            _masterData = masterData;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> MasterKeys()
        {
            var masterKeys = await _masterData.GetAllMasterKeysAsync();
            var masterKeysViewModel = _mapper.Map<List<MasterDataKey>, List<MasterDataKeyViewModel>>(masterKeys);

            HttpContext.Session.SetSession("MasterKeys", masterKeysViewModel);

            return View(new MasterKeysViewModel
            {
                MasterKeys = masterKeysViewModel?.ToList(),
                IsEdit = false
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterKeys(MasterKeysViewModel masterKeys)
        {
            masterKeys.MasterKeys = HttpContext.Session.GetSession<List<MasterDataKeyViewModel>>("MasterKeys");
            if (!ModelState.IsValid)
            {
                return View(masterKeys);
            }

            var masterKey = _mapper.Map<MasterDataKeyViewModel, MasterDataKey>(masterKeys.MasterKeyInContext);
            if (masterKeys.IsEdit)
            {
                await _masterData.UpdateMasterKeyAsync(masterKeys.MasterKeyInContext.PartitionKey, masterKey);
            }
            else
            {
                masterKey.RowKey = Guid.NewGuid().ToString();
                masterKey.PartitionKey = masterKey.Name;
                masterKey.CreatedBy = HttpContext.User.GetCurrentUserDetails().Name;
                await _masterData.InsertMasterKeyAsync(masterKey);
            }

            return RedirectToAction("MasterKeys");
        }

        [HttpGet]
        public async Task<IActionResult> MasterValues()
        {
            ViewBag.MasterKeys = await _masterData.GetAllMasterKeysAsync();
            return View(new MasterValuesViewModel
            {
                MasterValues = new List<MasterDataValueViewModel>(),
                IsEdit = false
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterValues(bool isEdit, MasterDataValueViewModel masterValue)
        {
            if (!ModelState.IsValid)
            {
                return Json("Error");
            }

            var masterDataValue = _mapper.Map<MasterDataValueViewModel, MasterDataValue>(masterValue);

            if (isEdit)
            {
                await _masterData.UpdateMasterValueAsync(masterDataValue.PartitionKey, masterDataValue.RowKey, masterDataValue);
            }
            else
            {
                masterDataValue.RowKey = Guid.NewGuid().ToString();
                masterDataValue.CreatedBy = HttpContext.User.GetCurrentUserDetails().Name;
                await _masterData.InsertMasterValueAsync(masterDataValue);
            }

            return Json(true);
        }

        [HttpGet]
        public async Task<IActionResult> MasterValuesByKey(string key)
        {
            return Json(new { data = await _masterData.GetAllMasterValuesByKeyAsync(key) });
        }

        private async Task<List<MasterDataValue>> ParseMasterDataExcel(IFormFile excelFile)
        {
            var masterValueList = new List<MasterDataValue>();

            using (var memoryStream = new MemoryStream())
            {
                await excelFile.CopyToAsync(memoryStream);

                try
                {
                    using (ExcelPackage package = new ExcelPackage(memoryStream))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                        int rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var masterDataValue = new MasterDataValue
                            {
                                RowKey = Guid.NewGuid().ToString(),
                                PartitionKey = worksheet.Cells[row, 1]?.Value?.ToString(),
                                Name = worksheet.Cells[row, 2]?.Value?.ToString(),
                                IsActive = bool.TryParse(worksheet.Cells[row, 3]?.Value?.ToString(), out var isActive) ? isActive : false
                            };
                            masterValueList.Add(masterDataValue);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý tệp Excel");
                    throw new Exception("Tệp Excel không hợp lệ. Vui lòng kiểm tra lại định dạng hoặc dữ liệu trong tệp.");
                }
            }

            return masterValueList;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel()
        {
            var files = Request.Form.Files;
            if (!files.Any())
            {
                return Json(new { Error = true, Text = "Vui lòng tải lên một tệp Excel." });
            }

            var excelFile = files.First();
            if (excelFile.Length <= 0)
            {
                return Json(new { Error = true, Text = "Tệp không hợp lệ hoặc rỗng." });
            }

            try
            {
                var masterData = await ParseMasterDataExcel(excelFile);
                var result = await _masterData.UploadBulkMasterData(masterData);
                return Json(new { Success = result, Text = "Import thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi upload Excel");
                return Json(new { Error = true, Text = ex.Message });
            }
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}