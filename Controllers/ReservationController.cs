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
    public class ReservationController : ControllerBase
    {
        private readonly IDriver _driver;

        public ReservationController(IDriver driver)
        {
            _driver = driver;
        }

        [HttpPost]
        public async Task<IActionResult> AddReservation(int userId, Reservation reservation)
        {
            try
            {
                using (var session = _driver.AsyncSession())
                {
                    
                    var query = @"
                        MATCH (u:User) WHERE ID(u) = $userId
                        CREATE (r:Reservation { reservationDate: $reservationDate, duration: $duration })
                        CREATE (u)-[:MAKES]->(r)";
                    
                    var parameters = new
                    {
                        userId = userId,
                        reservationDate = reservation.ReservationDate,
                        duration = reservation.Duration
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

        [HttpGet("AllReservations")]
        public async Task<IActionResult> AllReservations()
        {
            try
            {
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.ReadTransactionAsync(async tx =>
                    {
                        var query = "MATCH (n:Reservation) RETURN n";
                        var cursor = await tx.RunAsync(query);
                        var nodes = new List<INode>();

                        await cursor.ForEachAsync(record =>
                        {
                            var node = record["n"].As<INode>();
                            nodes.Add(node);
                        });

                        return nodes;
                    });

                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpDelete]
        public async Task<IActionResult> RemoveReservation(int reservationId)
        {
            try
            {
                using (var session = _driver.AsyncSession())
                {
                    var query = @"MATCH (a:Reservation) where ID(a)=$aId
                                OPTIONAL MATCH (a)-[r]-()
                                DELETE r,a";
                    var parameters = new { aId = reservationId };
                    await session.RunAsync(query, parameters);
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("UpdateReservation")]
        public async Task<IActionResult> UpdateReservation(int reservationId, int newDuration, DateTime newReservationDate)
        {
            try
            {
                using (var session = _driver.AsyncSession())
                {
                    var query = @"MATCH (n:Reservation) WHERE ID(n)=$aId
                                SET n.duration=$duration
                                SET n.reservationDate=$reservationDate
                                RETURN n";
                    var parameters = new { aId = reservationId,
                                        duration = newDuration,
                                        reservationDate = newReservationDate };
                    await session.RunAsync(query, parameters);
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


