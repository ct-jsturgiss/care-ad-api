using CareAdApi.Helpers;
using CareAdApi.Models;
using CareAdApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        public async Task<JsonResult> UpdateAttributesAsync([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] JsonObject rawJson)
        {
            try
            {
                if(rawJson == null)
                {
                    return NoUpdates().Result;
                }
                AttributeUpdateResponse? resp = ValidateRequestKeys(rawJson);
                if(resp != null)
                {
                    return new JsonResult(resp) { StatusCode = (int)HttpStatusCode.BadRequest };
                }

                AttributeUpdateRequest? request = JsonSerializer.Deserialize<AttributeUpdateRequest>(rawJson);
                if(request == null || request.Updates.Count == 0)
                {
                    return NoUpdates().Result;
                }

                resp = await m_adService.UpdateAttributesAsync(request.Updates);

                return new JsonResult(resp);
            }
            catch(Exception ex)
            {
                return Error(ex).Result;
            }
            finally { }
        }

        private AttributeUpdateResponse? ValidateRequestKeys(JsonObject obj)
        {
            List<ProcessError> errs = new List<ProcessError>();
            string[] rootKeys = obj.Select(x => x.Key).ToArray();
            string[] expectedRootKeys = JsonHelper.GetSerializedKeys<AttributeUpdateRequest>();
            string[] unrecognizedRootKeys = rootKeys.Except(expectedRootKeys).ToArray();
            errs.AddRange(unrecognizedRootKeys.Select(x => new ProcessError() { ErrorType = ErrorType.Unknown, Messages = [$"'{x}' is not an expected key."] }));
            if(obj.Count > 0)
            {
                JsonArray? arr = obj.FirstOrDefault().Value?.AsArray();
                if(arr != null && arr.Count > 0)
                {
                    string[] objKeys = arr.SelectMany(x => x.AsObject().Select(x => x.Key)).Distinct().ToArray();
                    string[] expectedObjKeys = JsonHelper.GetSerializedKeys<AttributesUpdate>();
                    string[] unrecognizedObjKeys = objKeys.Except(expectedObjKeys).ToArray();
                    errs.AddRange(unrecognizedObjKeys.Select(x => new ProcessError() { ErrorType = ErrorType.Unknown, Messages = [$"'{x}' is not an expected key."] }));
                }
            }
            if(errs.Count > 0)
            {
                return new AttributeUpdateResponse() { Errors = errs };
            }

            return null;
        }

        private Task<JsonResult> NoUpdates()
        {
            ProcessError[] errs = [new ProcessError() {
                ErrorType = ErrorType.User,
                Messages = ["No updates provided."]
            }];

            JsonResult res = new JsonResult(new AttributeUpdateResponse() { Errors = errs.ToList() });
            res.StatusCode = (int)HttpStatusCode.BadRequest;

            return Task.FromResult(res);
        }

        private Task<JsonResult> Error(Exception ex)
        {
            ProcessError[] errs = [new ProcessError() {
                ErrorType = ErrorType.Unknown,
                Messages = [ex.Message]
            }];

            JsonResult res = new JsonResult(new AttributeUpdateResponse() { Errors = errs.ToList() });
            res.StatusCode = (int)HttpStatusCode.InternalServerError;

            return Task.FromResult(res);
        }
    }
}
