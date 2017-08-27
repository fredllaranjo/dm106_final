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
using DM106_final.CRMClient;
using DM106_final.br.com.correios.ws;

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
            if (order == null)
            {
                return NotFound();
            }
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
            if (order == null)
            {
                return NotFound();
            }
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
            order.status = "fechado";
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
        [HttpPost]
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
        [HttpDelete]
        [ResponseType(typeof(Order))]
        public IHttpActionResult DeleteOrder(int id)
        {
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return NotFound();
            }
            if (isInvalidUser(order.userName))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            db.Orders.Remove(order);
            db.SaveChanges();

            return Ok(order);
        }

        // PUT: api/Orders/calcfrete/5
        [Authorize]
        [HttpPut]
        [Route("calcFreteOrder")]
        [ResponseType(typeof(Order))]
        public IHttpActionResult CalcFreteOrder(int id)
        {
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return NotFound();
            }
            if (isInvalidUser(order.userName))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }
            if (!"novo".Equals(order.status))
            {
                return BadRequest("Impossível calcular frete para pedido com status: " + order.status);
            }
            if (order.OrderItems.Count() <= 0)
            {
                return BadRequest("Impossível calcular frete para pedido sem itens.");
            }
            decimal precoItens = 0;
            decimal pesoItens = 0;
            decimal totalComprimento = 0;
            decimal maiorLargura = 0;
            decimal maiorAltura = 0;
            decimal maiorDiametro = 0;
            foreach (OrderItem orderItem in order.OrderItems)
            {
                //Get Product statuses
                Product product = db.Products.Find(orderItem.ProductId);
                precoItens += product.preco * orderItem.Quantidade;
                pesoItens += product.peso * orderItem.Quantidade;
                totalComprimento += product.comprimento * orderItem.Quantidade;
                if (product.largura > maiorLargura)
                {
                    maiorLargura = product.largura;
                }
                if (product.altura > maiorAltura)
                {
                    maiorAltura = product.altura;
                }
                if (product.diametro > maiorDiametro)
                {
                    maiorDiametro = product.diametro;
                }
            }
            Double precoFrete = 0;
            Double prazoEntrega = 0;
            string cepDestino = ObtemCEP(order.userName);
            if (cepDestino != null)
            {
                cServico precoPrazo = ObtemFrete(cepDestino, totalComprimento, maiorLargura, maiorAltura, maiorDiametro, precoItens);
                if (precoPrazo != null && precoPrazo.MsgErro == null)
                {
                    precoFrete = Convert.ToDouble(precoPrazo.Valor.Replace(",","."));
                    prazoEntrega = Convert.ToDouble(precoPrazo.PrazoEntrega);
                }
                else
                {
                    //Erro ao consultar WS Correios
                    return BadRequest(precoPrazo.MsgErro);
                }
            }
            else
            {
                //Erro ao consultar CRM
                return BadRequest("Erro ao obter o cep destino - CRM indisponível ou usuário não encontrado");
            }

            order.pesoTotalPedido = pesoItens;
            order.precoFrete = (decimal)precoFrete;
            order.precoTotalPedido = (decimal)precoFrete + precoItens;

            DateTime dataPedido = DateTime.ParseExact(order.dataPedido, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);
            order.dataEntrega = dataPedido.AddDays(prazoEntrega).ToString("dd/MM/yyyy");

            db.Entry(order).State = EntityState.Modified;
            db.SaveChanges();

            return Ok(order);
        }

        private cServico ObtemFrete(string cepDestino, decimal comprimento, decimal largura, decimal altura, decimal diametro, decimal valorDeclarado)
        {
            CalcPrecoPrazoWS correiosWS = new CalcPrecoPrazoWS();
            cResultado resultado = correiosWS.CalcPrecoPrazo("", "", "40010", "37540000", cepDestino, "1", 1, comprimento, largura, altura, diametro, "N", valorDeclarado, "S");
            if (resultado != null)
            {
                if (!resultado.Servicos[0].Erro.Equals("0"))
                {
                    resultado.Servicos[0].MsgErro = "Erro ao calcular o frete - Código do erro WS Correios: " + resultado.Servicos[0].Erro + "-" + resultado.Servicos[0].MsgErro;

                }
                else
                {
                    resultado.Servicos[0].MsgErro = null;
                }
            }
            else
            {
                resultado.Servicos[0].MsgErro = "Erro ao calcular o frete - WS Correios indisponível";
            }
            return resultado.Servicos[0];
        }

        private string ObtemCEP(string email)
        {
            CRMRestClient crmClient = new CRMRestClient();
            Customer customer = crmClient.GetCustomerByEmail(email);
            if (customer != null)
            {
                return customer.zip;
            }
            else
            {
                return null;
            }
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
            return username == null || (!User.IsInRole("ADMIN") && !User.Identity.Name.Equals(username));
        }

        private bool OrderExists(int id)
        {
            return db.Orders.Count(e => e.Id == id) > 0;
        }
    }
}