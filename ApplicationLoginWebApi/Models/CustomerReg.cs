using System;
using System.Collections.Generic;

namespace ApplicationLoginWebApi.Models
{
    public partial class CustomerReg
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailId { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string TokenId { get; set; }
        public long? MobileNo { get; set; }
        public DateTime? Date { get; set; }
        public string UserName { get; set; }
        public int? Count { get; set; }
        public bool? Status { get; set; }
    }
}
