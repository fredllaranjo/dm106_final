using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using DM106_final.Models;

namespace DM106_final.Controllers
{
    [RoutePrefix("api/Orders")]
    public class OrdersController : ApiController
    {
        private DM106_final_Private_Context db = new DM106_final_Private_Context();

        // GET: api/Orders
        [Authorize(Roles = "ADMIN")]
        public List<Order> GetOrders()
        {
            return db.Orders.Include(order => order.OrderItems).ToList();
        }

        // GET: api/Orders/5
        [Authorize]
        [ResponseType(typeof(Order))]
        public IHttpActionResult GetOrder(int id)
        {
            Order order = db.Orders.Find(id);
            if (isInvalidUser(order.userName))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }
            if (order == null)
            {
                return NotFound();
            }

            return Ok(order);
        }

        // GET: api/Orders/byEmail
        [Authorize]
        [ResponseType(typeof(Order))]
        [HttpGet]
        [Route("byEmail")]
        public IHttpActionResult GetOrderByEmail(string email)
        {
            IQueryable<Order> orders = db.Orders.Where(p => p.userName == email);
            if (isInvalidUser(email))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }
            if (orders == null)
            {
                return StatusCode(HttpStatusCode.NoContent);
            }

            return Ok(orders);
        }

        // PUT: api/Orders/closeOrder/5
        [Authorize]
        [HttpPut]
        [Route("closeOrder")]
        [ResponseType(typeof(void))]
        public IHttpActionResult CloseOrder(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            Order order = db.Orders.Find(id);
            if (isInvalidUser(order.userName))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }
            if (id != order.Id)
            {
                return BadRequest();
            }
            if (order.precoFrete == 0)
            {
                var response = new HttpResponseMessage(HttpStatusCode.PreconditionFailed);

                response.ReasonPhrase = "O Frete deve ser calculado antes do fechamento do pedido";
                return ResponseMessage(response);
            }
            db.Entry(order).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Orders
        [Authorize]
        [ResponseType(typeof(Order))]
        public IHttpActionResult PostOrder(Order order)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (isInvalidUser(order.userName))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            //Overwrite new order to default values
            order.status = "novo";
            order.pesoTotalPedido = 0;
            order.precoFrete = 0;
            order.precoTotalPedido = 0;
            order.dataPedido = DateTime.Now.ToString("dd/MM/yyyy");

            db.Orders.Add(order);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = order.Id }, order);
        }

        // DELETE: api/Orders/5
        [Authorize]
        [ResponseType(typeof(Order))]
        public IHttpActionResult DeleteOrder(int id)
        {
            Order order = db.Orders.Find(id);
            if (isInvalidUser(order.userName))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }
            if (order == null)
            {
                return NotFound();
            }

            db.Orders.Remove(order);
            db.SaveChanges();

            return Ok(order);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool isInvalidUser(string username)
        {
            return !User.IsInRole("ADMIN") && !User.Identity.Name.Equals(username);
        }

        private bool OrderExists(int id)
        {
            return db.Orders.Count(e => e.Id == id) > 0;
        }
    }
}