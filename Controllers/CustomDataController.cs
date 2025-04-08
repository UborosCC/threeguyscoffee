using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Easyweb.Controllers;

public class CustomDataController : Controller
{
    [HttpGet("/data")]
    public virtual async Task<IActionResult> Index()
    {
        string result;

        using (var client = new HttpClient())
        {
            var response = await client.GetAsync("https://swapi.dev/api/people/1/");
            result = await response.Content.ReadAsStringAsync();
        }

        return Ok(result);
    }
}
