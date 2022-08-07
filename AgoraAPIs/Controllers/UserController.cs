using AgoraDatabase;
using AgoraDatabase.Contexts;
using AgoraDatabase.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoraAPIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IDataService<UserData> dataService = new GenericDataService<UserData>(new UserDataContextFactory());

        // Get request
        [HttpGet("{username:string}")]
        public async Task<ActionResult<UserData>> GetUser(string username)
        {
            try
            {
                var result = await dataService.Get(username);
                if (result == null)
                {
                    return NotFound();
                }
                return result;
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Failed to retrieve from database");
            }
        }

        // Post request
        [HttpPost]
        public async Task<ActionResult<UserData>> CreateUser(UserData newUser)
        {
            try
            {
                if (newUser == null)
                {
                    return BadRequest();
                }

                var testDup = dataService.Get(newUser.UserName);
                if (testDup != null)
                {
                    return BadRequest();
                }

                var result = await dataService.Create(newUser);
                return CreatedAtAction(nameof(GetUser),
                    new { username = result.UserName });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Failed to retrieve from database");
            }
        }

        // Put request
        [HttpPut("{username:string}/{updateString}")]
        public async Task<ActionResult<UserData>> UpdateUser(UserData user, string username, string updateString)
        {
            try
            {
                if (username != user.UserName)
                {
                    return BadRequest();
                }

                var toUpdate = dataService.Get(user.UserName);
                if (toUpdate == null)
                {
                    return NotFound($"No user with username {username}");
                }

                return await dataService.Update(updateString, toUpdate.Result);
                
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Failed to retrieve from database");
            }
        }
    }
}
