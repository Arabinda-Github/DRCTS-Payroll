using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;

namespace HR_Payroll.API.JWTExtension
{
    public static class ExceptionMiddlewareExtensions
    {
        public static void ConfigureExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        await context.Response.WriteAsync(new ErrorResponse()
                        {
                            status = false,
                            message = "Internal Server Error."
                        }.ToString());
                    }
                });
            });
        }

        public static void ConfigureRedundantStatusCodePages(this IApplicationBuilder app)
        {
            // This is redundant but placed here to make the payload to say what the HTTP response is to make it easier to read.
            app.UseStatusCodePages(async context =>
            {
                context.HttpContext.Response.ContentType = "application/json";

                var responsePayload = new ErrorResponse()
                {
                    message = ReasonPhrases.GetReasonPhrase(context.HttpContext.Response.StatusCode)
                };

                await context.HttpContext.Response.WriteAsync(responsePayload.ToString());
            });
        }
    }
}
