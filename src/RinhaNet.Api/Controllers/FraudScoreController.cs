using Microsoft.AspNetCore.Mvc;

namespace RinhaNet.Api.Controllers
{
    [ApiController]
    [Route("fraud-score")]
    public class FraudScoreController : ControllerBase
    {
        [HttpPost]
        public IActionResult FraudScore()
        {
            return Ok();
        }
    }
}
