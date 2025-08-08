using Microsoft.AspNetCore.Mvc;

namespace StockApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BacktestController : ControllerBase
    {
        [HttpPost]
        public IActionResult Post([FromBody] object request)
        {
            // TODO: integrate with backtest engine
            return Ok("Backtest endpoint not implemented yet");
        }
    }
}
