using CareAdApi.Models;
using CareAdApi.Services;
using Microsoft.AspNetCore.Mvc;

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
                await Task.Delay(0);
                return new JsonResult(new AttributeUpdateResponse()
                {
                    Success = updates.Select(x => x.PrincipalName!).ToList(),
                    Errors = updates.Select(x => 
                        new ProcessError() { 
                            ErrorType = ErrorType.User, 
                            UserPrincipalName = x.PrincipalName!, 
                            Messages = ["Random Error 1", "Random Error 2"] }
                    ).ToList()
                });
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
            return Task.FromResult(new JsonResult(errs));
        }
    }
}
