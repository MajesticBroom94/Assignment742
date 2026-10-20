namespace Assignment742.Controllers;

    
using Microsoft.AspNetCore.Mvc;

public class ErrorController : Controller
{
    [Route("Error/{statusCode:int}")]
    public IActionResult HttpStatusCode(int statusCode)
    {
        Response.StatusCode = statusCode;

        ViewBag.StatusCode = statusCode;

        ViewBag.Message = statusCode switch
        {
            400 => "The request could not be processed.",
            401 => "You must log in to access this page.",
            403 => "You do not have permission to access this page.",
            404 => "The page you requested could not be found.",
            500 => "An unexpected server error occurred.",
            _ => "Something went wrong."
        };

        return View("Error");
    }
}

