using Microsoft.AspNetCore.Mvc;

namespace CoreGateway.Storage.Service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileController : ControllerBase
    {
        private readonly ILogger<FileController> _logger;

        public FileController(ILogger<FileController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> GetAsync(Guid fileGuid)
        {
            var stream = new MemoryStream();
            stream.Write(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 });
            stream.Position = 0;
            return File(stream,
                "application/octet-stream",
                "some_file.bin");
        }

        [HttpPut]
        public async Task<ActionResult<Guid>> PutAsync(IFormFile data)
        {
            if (data == null)
                return new BadRequestObjectResult(
                    new ArgumentNullException(nameof(data)));

            var buffer = new byte[1024];
            var totalCount = 0;
            using var stream = data.OpenReadStream();
            while (true)
            {
                var count = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (count == 0)
                    break;
                totalCount += count;
            }

            return new ActionResult<Guid>(Guid.NewGuid());
        }
    }
}
