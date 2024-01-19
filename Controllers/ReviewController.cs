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
    public class ReviewController : ControllerBase
    {
        private readonly IDriver _driver;

        public ReviewController(IDriver driver)
        {
            _driver = driver;
        }

        [HttpPost("AddReview")]
        public async Task<IActionResult> AddReview(Review review)
        {
            try
            {
                using (var session = _driver.AsyncSession())
                {
                    
                    var query = @"
                        CREATE (r:Review {
                            Id: $Id,
                            rating: $rating,
                            comment: $comment
                        })";
                    
                    var parameters = new
                    {
                        Id = Guid.NewGuid().ToString(),
                        rating = review.Rating,
                        comment = review.Comment
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

        [HttpPost("GiveReview")]
        public async Task<IActionResult> GiveReview(int userId, int reviewId)
        {
            try
            {
                using (var session = _driver.AsyncSession())
                {
                    
                    var query = @"MATCH (u:User) WHERE ID(u) = $uId
                                MATCH (r:Review) WHERE ID(r) = $rId
                                CREATE (u)-[:GIVES]->(r)";
                    
                    var parameters = new
                    {
                        uId = userId,
                        rId=reviewId
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


        [HttpGet("AllReviews")]
        public async Task<IActionResult> AllReviews()
        {
            try
            {
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.ReadTransactionAsync(async tx =>
                    {
                        var query = "MATCH (n:Review) RETURN ID(n) as reviewId, n";
                        var cursor = await tx.RunAsync(query);
                        var reviews = new List<object>();

                        await cursor.ForEachAsync(record =>
                        {
                            var review = new Dictionary<string, object>();
                            review.Add("reviewId", record["reviewId"].As<long>());

                            var node = record["n"].As<INode>();
                            var reviewAttributes = new Dictionary<string, object>();

                            foreach (var property in node.Properties)
                            {
                                reviewAttributes.Add(property.Key, property.Value);
                            }

                            review.Add("attributes", reviewAttributes);
                            reviews.Add(review);
                        });

                        return reviews;
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
        public async Task<IActionResult> RemoveReview(int reviewId)
        {
            try
            {
                using (var session = _driver.AsyncSession())
                {
                    var checkReviewQuery = "MATCH (r:Review) WHERE ID(r) = $aId RETURN COUNT(r) as count";
                    var checkReviewParameters = new { aId = reviewId };
                    var result = await session.RunAsync(checkReviewQuery, checkReviewParameters);

                    var count = await result.SingleAsync(r => r["count"].As<int>());

                    if (count == 0)
                    {
                        return NotFound($"Review with ID {reviewId} does not exist.");
                    }

                    var query = @"MATCH (a:Review) where ID(a)=$aId
                                OPTIONAL MATCH (a)-[r]-()
                                DELETE r,a";
                    var parameters = new { aId = reviewId };
                    await session.RunAsync(query, parameters);
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("UpdateReview")]
        public async Task<IActionResult> UpdateReview(int reviewId, int newRating, string newComment)
        {
            try
            {
                using (var session = _driver.AsyncSession())
                {
                    var checkReviewQuery = "MATCH (r:Review) WHERE ID(r) = $aId RETURN COUNT(r) as count";
                    var checkReviewParameters = new { aId = reviewId };
                    var result = await session.RunAsync(checkReviewQuery, checkReviewParameters);

                    var count = await result.SingleAsync(r => r["count"].As<int>());

                    if (count == 0)
                    {
                        return NotFound($"Review with ID {reviewId} does not exist.");
                    }
                    
                    var query = @"MATCH (n:Review) WHERE ID(n)=$aId
                                SET n.rating=$rating
                                SET n.comment=$comment
                                RETURN n";
                    var parameters = new { aId = reviewId,
                                        rating = newRating,
                                        comment = newComment };
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


