using Microsoft.AspNetCore.Mvc;
using WebApplication1.Dtos;
using WebApplication1.Interfaces.IPseudoGeneratorService;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabOneController : ControllerBase
    {
        private readonly IPseudoGeneratorService _pseudoGenService;
        public LabOneController(IPseudoGeneratorService pseudoGenService)
        {
            _pseudoGenService = pseudoGenService;
        }


        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] long m, [FromQuery] long a, [FromQuery] long c, [FromQuery] long x0, [FromQuery] long n)
        {
            var (seq, randomSeq) = await _pseudoGenService.Generate(a , m, n, c, x0);
            var period = await _pseudoGenService.GetPeriod(seq);
            var periodRandom = await _pseudoGenService.GetPeriod(randomSeq);
            var cesaroRatio = await _pseudoGenService.SequenceEstimation(seq);
            var cesaroRandomRatio = await _pseudoGenService.SequenceEstimation(randomSeq);
            LabOneDto result = new LabOneDto
            {
                Seq = seq,
                RandomSeq = randomSeq,
                Period = period,
                PeriodRandom = periodRandom,
                CesaroRatio = cesaroRatio,
                CesaroRandomRatio = cesaroRandomRatio
            };
            return Ok(result);
        }
    }
}
