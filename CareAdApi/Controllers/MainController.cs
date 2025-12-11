using CareAdApi.Models;
using CareAdApi.Services;
using Microsoft.AspNetCore.Mvc;
using ILogger = Serilog.ILogger;

namespace CareAdAsync.Controllers
{
    [Route("/api/ad/")]
    [Controller]
    public class MainController
    {
        private ILogger m_logger = null!;
        private ActiveDirectoryService m_adService = null!;

        public MainController(ILogger logger, ActiveDirectoryService adService)
        {
            m_logger = logger;
            m_adService = adService;
        }

        [Route("attributes")]
        [HttpPost()]
        public async Task<JsonResult> UpdateAttributesAsync([FromBody] AttributesUpdate[] updates)
        {
            try
            {
                updates = updates ?? [];
                AttributeUpdateResponse resp = await m_adService.UpdateAttributesAsync(updates);

                return new JsonResult(resp);
            }
            catch(Exception ex)
            {
                return Error(ex).Result;
            }
            finally { }
        }

        private Task<JsonResult> Error(Exception ex)
        {
            ProcessError[] errs = [new ProcessError() {
                ErrorType = ErrorType.Unknown,
                Messages = [ex.Message]
            }];
            return Task.FromResult(new JsonResult(new { errors = errs }));
        }
    }
}
