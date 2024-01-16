using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Databaseaccess.Models;


namespace Databaseaccess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IDriver _driver;

        public UserController(IDriver driver)
        {
            _driver = driver;
        }

    [HttpPost("RegisterUser")]
    public async Task<IActionResult> RegisterUser(User user)
    {
        try
        {
            
            if (await IsEmailTaken(user.Email))
            {
                return BadRequest("Email is already taken.");
            }

            
            var addUserResult = await AddUser(user);

            
            if (addUserResult is OkResult)
            {
                return Ok("User registered successfully.");
            }
            else
            {
                
                return addUserResult;
            }
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("AddUser")]
    public async Task<IActionResult> AddUser(User user)
    {
        try
        {
            
            if (await IsEmailTaken(user.Email))
            {
                return BadRequest("Email is already taken.");
            }

            using (var session = _driver.AsyncSession())
            {
                var query = @"CREATE (n:User { username: $username, email: $email, password: $password, role: $role})";

                var parameters = new
                {
                    username = user.Username,
                    email = user.Email,
                    password = user.Password, 
                    role = user.Role,
                };

                await session.RunAsync(query, parameters);
                return Ok();
            }
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private async Task<bool> IsEmailTaken(string email)
    {
        using (var session = _driver.AsyncSession())
        {
            var query = "MATCH (n:User {email: $email}) RETURN COUNT(n) as count";
            var parameters = new { email };

            var result = await session.RunAsync(query, parameters);
            var count = await result.SingleAsync(r => r["count"].As<int>());

            return count > 0;
        }
    }

    
    [HttpPost("LoginUser")]
    public async Task<IActionResult> LoginUser(string username, string password)
    {
        try
        {
            using (var session = _driver.AsyncSession())
            {
                
                var query = "MATCH (n:User {username: $username, password: $password}) RETURN ID(n) as userId, n";
                var parameters = new { username, password };

                var result = await session.RunAsync(query, parameters);

                if (await result.FetchAsync())
                {
                    var userId = result.Current["userId"].As<long>();
                    return Ok($"User with ID {userId} successfully logged in.");
                }
                else
                {
                    return Unauthorized("Invalid username or password.");
                }
            }
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }


    //    [HttpPost]
    //     public async Task<IActionResult> AddUser(User user)
    //     {
    //         try
    //         {
    //             using (var session = _driver.AsyncSession())
    //             {
    //                 var query = @"CREATE (n:User { username: $username, email: $email, password: $password, role: $role})";

    //                 var parameters = new
    //                 {
    //                     username = user.Username,
    //                     email = user.Email,
    //                     password = user.Password,
    //                     role = user.Role,
    //                 };
                    
    //                 await session.RunAsync(query, parameters);
    //                 return Ok();
                    
    //             }
    //         }
    //         catch (Exception ex)
    //         {
    //             return BadRequest(ex.Message);
    //         }
    //     }


        [HttpDelete]
        public async Task<IActionResult> RemoveUser(int userId)
        {
            try
            {
                using (var session = _driver.AsyncSession())
                {
                    
                    var checkUserQuery = "MATCH (a:User) WHERE ID(a) = $aId RETURN COUNT(a) as count";
                    var checkUserParameters = new { aId = userId };
                    var result = await session.RunAsync(checkUserQuery, checkUserParameters);

                    var count = await result.SingleAsync(r => r["count"].As<int>());

                    if (count == 0)
                    {
                        return NotFound($"User with ID {userId} does not exist.");
                    }

                    var deleteQuery = @"MATCH (a:User) WHERE ID(a)=$aId
                                        OPTIONAL MATCH (a)-[r]-()
                                        DELETE r, a";

                    var deleteParameters = new { aId = userId };
                    await session.RunAsync(deleteQuery, deleteParameters);

                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        
        [HttpGet("AllUsers")]
        public async Task<IActionResult> AllUsers()
        {
            try
            {
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.ReadTransactionAsync(async tx =>
                    {
                        var query = "MATCH (n:User) RETURN ID(n) as userId, n";
                        var cursor = await tx.RunAsync(query);
                        var users = new List<object>();

                        await cursor.ForEachAsync(record =>
                        {
                            var user = new Dictionary<string, object>();
                            user.Add("userId", record["userId"].As<long>());

                            var node = record["n"].As<INode>();
                            var userAttributes = new Dictionary<string, object>();

                            foreach (var property in node.Properties)
                            {
                                userAttributes.Add(property.Key, property.Value);
                            }

                            user.Add("attributes", userAttributes);
                            users.Add(user);
                        });

                        return users;
                    });

                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("UpdateUser")]
        public async Task<IActionResult> UpdateUser(int userId, string newUsername, string newEmail, string newPassword, string newRole)
        {
            try
            {
                using (var session = _driver.AsyncSession())
                {
                    
                    var checkUserQuery = "MATCH (n:User) WHERE ID(n) = $aId RETURN COUNT(n) as count";
                    var checkUserParameters = new { aId = userId };
                    var result = await session.RunAsync(checkUserQuery, checkUserParameters);

                    var count = await result.SingleAsync(r => r["count"].As<int>());

                    if (count == 0)
                    {
                        return NotFound($"User with ID {userId} does not exist.");
                    }

                    
                    var updateQuery = @"MATCH (n:User) WHERE ID(n)=$aId
                                        SET n.username=$username
                                        SET n.email=$email
                                        SET n.password=$password
                                        SET n.role=$role
                                        RETURN n";

                    var updateParameters = new
                    {
                        aId = userId,
                        username = newUsername,
                        email = newEmail,
                        password = newPassword,
                        role = newRole
                    };

                    await session.RunAsync(updateQuery, updateParameters);
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        
    }
}