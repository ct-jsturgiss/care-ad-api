
using CareAdApi.Configuration;
using Microsoft.Extensions.Primitives;
using Serilog;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using ILogger = Serilog.ILogger;

namespace CareAdApi.Services
{
    public class BasicAuthMiddleware : IMiddleware
    {
        private ILogger m_logger = null!;
        private LocalConfigFile m_file = null!;

        public BasicAuthMiddleware(ILogger logger, LocalConfigFile file)
        {
            m_logger = logger;
            m_file = file;
        }

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            HttpRequest req = context.Request;
            StringValues authVal = new StringValues();
            string suppliedKey = string.Empty;
            if(req.Headers.TryGetValue(Constants.Headers.HeaderKey, out authVal))
            {
                try
                {
                    suppliedKey = authVal.ToString();
                    string plain = Encoding.UTF8.GetString(Convert.FromBase64String(authVal.ToString()));
                    if (m_file.HeaderKey.Equals(plain, StringComparison.OrdinalIgnoreCase))
                    {
                        next(context);
                        return Task.CompletedTask;
                    }
                }
                catch(Exception ex)
                {
                    m_logger.Error(ex, "Failed to process basic auth key: {j:v}", GetRequesterInfo(context));
                }
            }

            var logData = GetRequesterInfo(context);
            logData["supplied_key"] = suppliedKey;
            m_logger.Error("Failed basic authorization check: {j:v}", logData);
            ReturnForbidden(context.Response);

            return Task.CompletedTask;
        }

        private void ReturnForbidden(HttpResponse response)
        {
            response.StatusCode = (int)HttpStatusCode.Forbidden;
            response.WriteAsync("FORDBIDDEN");
        }

        private JsonObject GetRequesterInfo(HttpContext context)
        {
            JsonObject obj = new JsonObject();
            obj["connection_id"] = context.Connection.Id;
            obj["remote_host"] = context.Connection.RemoteIpAddress?.ToString() ?? "N/A";
            obj["remote_port"] = context.Connection.RemotePort.ToString();

            return obj;
        }
    }
}
