using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rystem.Authentication.Social.TestApi.Controllers
{
    [Authorize]
    [Route("[Controller]/[Action]")]
    public class MovieController : Controller
    {
        public class Something
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
        }
        [HttpGet]
        public Something Index()
        {
            return new Something
            {

            };
        }
    }
}
