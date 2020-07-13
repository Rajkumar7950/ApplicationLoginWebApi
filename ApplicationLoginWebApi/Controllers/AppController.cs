using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApplicationLoginWebApi.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Net;

namespace ApplicationLoginWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppController : ControllerBase
    {
        private readonly DotnetApplicationContext _context;
        IConfiguration _config;
        public AppController(DotnetApplicationContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: api/App
        [HttpGet("Get")]
        public async Task<ActionResult<IEnumerable<CustomerReg>>> GetCustomerReg()
        {
            return await _context.CustomerReg.ToListAsync();
        }

        // GET: api/App/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerReg>> GetCustomerReg(int id)
        {
            var customerReg = await _context.CustomerReg.FindAsync(id);

            if (customerReg == null)
            {
                return NotFound();
            }

            return customerReg;
        }

        // PUT: api/App/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCustomerReg(int id, CustomerReg customerReg)
        {
            if (id != customerReg.Id)
            {
                return BadRequest();
            }

            _context.Entry(customerReg).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerRegExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/App
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        //[HttpPost("Reg")]
        //public async Task<ActionResult<CustomerReg>> PostCustomerReg(CustomerReg customerReg)
        //{
        //    _context.CustomerReg.Add(customerReg);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction("GetCustomerReg", new { id = customerReg.Id }, customerReg);
        //}
        [HttpPost("Reg")]
        public async Task<ActionResult<CustomerReg>> PostCustomerRegistration(CustomerReg cd)
        {
            //_context.CustomerRegistration.Add(CustomerRegistration);
            //await _context.SaveChangesAsync();

            //return CreatedAtAction("GetCustomerRegistration", new { id = customerRegistration.Id }, customerRegistration);
            try
            {
                CustomerReg c = new CustomerReg();
                c.FirstName = cd.FirstName;
                c.LastName = cd.LastName;
                c.EmailId = cd.EmailId;
                c.Password = CommonMethods.ConverttoEncrypt(cd.Password);
                c.ConfirmPassword = CommonMethods.ConverttoEncrypt(cd.ConfirmPassword);
                c.TokenId = cd.TokenId;
                c.MobileNo = cd.MobileNo;
                c.Date = cd.Date;
                c.Status = true;
                c.Count = 0;
                c.UserName = cd.UserName;
                //var user = _context.CustomerRegistration.Where(x => x.FirstName == cd.FirstName && x => x.FirstName == cd.LastName && x => x.EmailId == cd.EmailId && x.Password == CommonMethods.ConverttoEncrypt(cd.Password)); 
                _context.CustomerReg.Add(c);

                _context.SaveChanges();
                return Ok(
                  new
                  {
                      success = true,
                      status = 200,
                      data = "Registration successfully Completed please Login in to the Application"
                  }); ;
            }
            catch (Exception ex)
            {
                if (CustomerRegExists(cd.Id))
                {
                    return Conflict();
                }
                else
                {
                    return Ok(new { success = false, status = 401, data = "Invalid Credentials" });
                }
            }
        }


        // DELETE: api/App/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<CustomerReg>> DeleteCustomerReg(int id)
        {
            var customerReg = await _context.CustomerReg.FindAsync(id);
            if (customerReg == null)
            {
                return NotFound();
            }

            _context.CustomerReg.Remove(customerReg);
            await _context.SaveChangesAsync();

            return customerReg;
        }

        private bool CustomerRegExists(int id)
        {
            return _context.CustomerReg.Any(e => e.Id == id);
        }

        [Route("loginApp")] // /login
        [HttpPost]
        public IActionResult Login(CustomerReg customer)
        {
            try
            {
                var value = _context.CustomerReg.Where(x => x.EmailId == customer.EmailId).FirstOrDefault();
                if (value != null)
                {
                    CustomerReg model = _context.CustomerReg.FirstOrDefault(x => x.EmailId == customer.EmailId);
                    var password = CommonMethods.ConvertToDecrypt(model.Password);
                    var data = _context.CustomerReg.Where(x => x.EmailId == customer.EmailId && password == customer.Password).FirstOrDefault();
                    if (data != null)
                    {
                        var accountStatus = _context.CustomerReg.Where(x => x.EmailId == customer.EmailId && x.Count >= 3).FirstOrDefault();
                        if (accountStatus == null)
                        {
                            var signinKey = new SymmetricSecurityKey(
                              Encoding.UTF8.GetBytes(_config["Jwt:SigningKey"]));
                            int expiryInMinutes = Convert.ToInt32(_config["Jwt:ExpiryInMinutes"]);
                            var token = new JwtSecurityToken(
                              issuer: _config["Jwt:Site"],
                              audience: _config["Jwt:Site"],
                              expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
                              signingCredentials: new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256)
                            );

                            var tokenData = new JwtSecurityTokenHandler().WriteToken(token);

                            model.TokenId = tokenData;
                            model.Date = DateTime.Now;
                            _context.Entry(model).State = EntityState.Modified;
                            _context.SaveChanges();
                            return Ok(
                              new
                              {
                                  token = new JwtSecurityTokenHandler().WriteToken(token),
                                  expiration = token.ValidTo,
                                  EmailIds = model.EmailId,
                                  status=200
                              });
                        }
                        else
                        {
                            model.Status = false;
                            _context.Entry(model).State = EntityState.Modified;
                            _context.SaveChanges();
                            return Ok(
                                  new
                                  {
                                      status = false,
                                      code = 401,
                                      message = "Account has been locked"
                                  });
                        }
                    }
                    else
                    {
                        model.Count = model.Count + 1;
                        _context.Entry(model).State = EntityState.Modified;
                        _context.SaveChanges();
                        var accountStatus = _context.CustomerReg.Where(x => x.EmailId == customer.EmailId && x.Count >= 3).FirstOrDefault();
                        if (accountStatus == null)
                        {
                            return Ok(
                                new
                                {
                                    status = false,
                                    code = 401,
                                    message = "Invalid credentials"
                                });
                        }
                        else
                        {
                            model.Status = false;
                            _context.Entry(model).State = EntityState.Modified;
                            _context.SaveChanges();
                            return Ok(
                                new
                                {
                                    status = false,
                                    code = 401,
                                    message = "Account has been locked"
                                });
                        }
                    }
                }
                else
                {
                    CustomerReg models = _context.CustomerReg.FirstOrDefault(x => x.EmailId == customer.EmailId);
                    models.Count = models.Count + 1;
                    _context.Entry(models).State = EntityState.Modified;
                    _context.SaveChanges();
                    return Ok(
                            new
                            {
                                status = false,
                                code = 401,
                                message = "Invalid credentials"
                            });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { status = ex });
            }
        }
        [HttpPost("AccountVerify")]
        public IActionResult AccountVerify(CustomerReg customer)
        {
            var user = _context.CustomerReg.Where(x => x.EmailId == customer.EmailId).FirstOrDefault();
            if (user != null)
            {
                CustomerReg model = _context.CustomerReg.FirstOrDefault(x => x.EmailId == customer.EmailId);
                var link = "http://localhost:4200/verify";

                var signinKey = new SymmetricSecurityKey(
                             Encoding.UTF8.GetBytes(_config["Jwt:SigningKey"]));
                int expiryInMinutes = Convert.ToInt32(_config["Jwt:ExpiryInMinutes"]);
                var token = new JwtSecurityToken(
                  issuer: _config["Jwt:Site"],
                  audience: _config["Jwt:Site"],
                  expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
                  signingCredentials: new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256)
                );

                var tokenData = new JwtSecurityTokenHandler().WriteToken(token);
                var fromEmail = new MailAddress("raj.vvit416@gmail.com", "Task For the verification");
                var toEmail = new MailAddress("raj.vvit416@gmail.com");
                var fromEmailPassword = "94417769222";
                string subject = "Activate account";
                string body = "Hi,<br/><br/> Kindly click on below link to activate your Account" +
                "<br/><br/><a href=" + link + ">Activate your account</a>" +
                "<br/><br/>Token=" + tokenData;

                model.TokenId = tokenData;
                _context.Entry(model).State = EntityState.Modified;
                _context.SaveChanges();

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword),
                };
                using (var message = new MailMessage(fromEmail, toEmail)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                    smtp.Send(message);
                return Ok(new { success = true, status = 200, data = "Kindly check your Email Id to activate account." });
            }
            else
            {
                return Ok(new { success = false, status = 401, data = "Unauthorized user" });
            }
        }
        [HttpPost("Activate")]
        public IActionResult ActivateAccount(CustomerReg customer)
        {
            var user = _context.CustomerReg.Where(x => x.EmailId == customer.EmailId &&  x.TokenId == customer.TokenId).FirstOrDefault();
            if (user != null)
            {
                CustomerReg model = _context.CustomerReg.FirstOrDefault(x => x.EmailId == customer.EmailId);
                model.Count = 0;
                model.Status = true;
                model.Date= null;
                model.TokenId = null;
                _context.Entry(model).State = EntityState.Modified;
                _context.SaveChanges();
                return Ok(new { success = true, status = 200, data = "Account has been activated." });
            }
            else
            {
                return Ok(
                   new
                   {
                       status = false,
                       code = 401,
                       message = "Invalid details"
                   });
            }
        }
        [HttpPost("Forgot")]
        public IActionResult ForgotPassword(CustomerReg customer)
        {
            var user = _context.CustomerReg.Where(x => x.EmailId == customer.EmailId).FirstOrDefault();
            if (user != null)
            {
                CustomerReg model = _context.CustomerReg.FirstOrDefault(x => x.EmailId == customer.EmailId);
                model.Date = DateTime.Now;
                _context.Entry(model).State = EntityState.Modified;
                _context.SaveChanges();
                var signinKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:SigningKey"]));
                int expiryInMinutes = Convert.ToInt32(_config["Jwt:ExpiryInMinutes"]);
                var token = new JwtSecurityToken(
                  issuer: _config["Jwt:Site"],
                  audience: _config["Jwt:Site"],
                  expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
                  signingCredentials: new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256)
                );
                string tokenData = new JwtSecurityTokenHandler().WriteToken(token);
                DateTime expiration = token.ValidTo;
                //var verifyUrl2 = "/Login/ResetPassword/";
                //var link2 = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl2);
                var link = "http://localhost:4200/reset";
                var fromEmail = new MailAddress("raj.vvit416@gmail.com", "Messsage");
                var toEmail = new MailAddress("raj.vvit416@gmail.com");
                var fromEmailPassword = "94417769222";
                string subject = "Reset Password";
                string body = "Hi,<br/><br/>We got request for reset your account password. Please find the token and click on the below link to reset your password" +
                    "<br/><br/><a href=" + link + ">Reset Password link</a> " +
                      "<br/><br/>Token=" + tokenData;

                model.TokenId = tokenData;
                _context.Entry(model).State = EntityState.Modified;
                _context.SaveChanges();
                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword),

                };
                using (var message = new MailMessage(fromEmail, toEmail)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                    smtp.Send(message);
                return Ok(new { success = true, status = 200, data = "Kindly check your Email Id to rest password." });
            }
            return Ok(new { success = false, status = 400, data = "Unauthorizes user" });
        }
        //[EnableCors("AllowOrigin")]
        [HttpPost("Reset")]
        public IActionResult ResetPassword(CustomerReg customer)
        {
            var user = _context.CustomerReg.Where(x => x.EmailId == customer.EmailId && x.TokenId == customer.TokenId).FirstOrDefault();

            if (user != null)
            {
                CustomerReg model = _context.CustomerReg.FirstOrDefault(x => x.EmailId == customer.EmailId);
                DateTime date = (DateTime)model.Date;
                TimeSpan duration = new TimeSpan(0, 0, 5, 0);
                DateTime value = date.Add(duration);
                if (DateTime.Now <= value)
                {
                    model.Password = CommonMethods.ConverttoEncrypt(customer.Password);
                    model.ConfirmPassword = CommonMethods.ConverttoEncrypt(customer.Password);
                    _context.Entry(model).State = EntityState.Modified;
                    _context.SaveChanges();
                    return Ok(new { success = true, status = 200, data = "Password has been changed. Kindly login with new password" });
                }
                else
                {
                    return Ok(new { success = false, status = 400, data = "Token has been expired. Please try again." });
                }
            }
            else
            {
                return Ok(new { success = false, status = 400, data = "Token or EmailId is invalid. Please try again." });
            }
        }
        [HttpPost("LogOut")]
        public IActionResult LogOut(CustomerReg c)
        {
            var user = _context.CustomerReg.Where(x => x.EmailId == c.EmailId).FirstOrDefault();

            if (user != null)
            {
                CustomerReg model = _context.CustomerReg.FirstOrDefault(x => x.EmailId == c.EmailId);
                model.TokenId = null;
                model.Date = null;
                _context.Entry(model).State = EntityState.Modified;
                _context.SaveChanges();
                return Ok(new { success = true, status = 200, data = "Suucessfullly logged out" });
            }
            else
            {
                return Ok(new { success = false, status = 400, data = "Unauthorizes user" });
            }


        }


    }
}

