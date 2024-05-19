using CoreGateway.Dispatcher.DataAccess;
using Microsoft.AspNetCore.Mvc;

namespace CoreGateway.Storage.Service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileController : ControllerBase
    {
        private readonly ILogger<FileController> _logger;
        private readonly IStorageDataAccess _dataAccess;

        public FileController(ILogger<FileController> logger, IStorageDataAccess dataAccess)
        {
            _logger = logger;
            _dataAccess = dataAccess;
        }

        [HttpGet]
        public async Task<ActionResult> GetAsync(Guid fileGuid)
        {
            var fileData = await _dataAccess.GetFile(fileGuid, CancellationToken.None);
            if (fileData?.Data == null)
                return base.BadRequest($"Файл [{fileGuid}] не найден.");
            var stream = new MemoryStream(fileData.Data);
            return File(stream,
                "application/octet-stream",
                Path.GetFileName(fileData.Name));
        }

        [HttpPut]
        public async Task<ActionResult<Guid>> PutAsync(IFormFile data)
        {
            if (data == null)
                return new BadRequestObjectResult(
                    new ArgumentNullException(nameof(data)));

            var ms = new MemoryStream();
            using var stream = data.OpenReadStream();
            await stream.CopyToAsync(ms, 1024);

            var result = await _dataAccess.InsertFile(Guid.NewGuid(), data.FileName, ms.ToArray(), CancellationToken.None);
            return result.Id;
        }
    }
}
