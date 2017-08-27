using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DM106_final.CRMClient
{
    public class Customer
    {
        public int Id { get; set; }
        [Required] [MaxLength(12)] public string cpf { get; set; }
        [Required] public string name { get; set; }
        [Required] public string address { get; set; }
        [Required] public string city { get; set; }
        [Required] public string state { get; set; }
        [Required] public string country { get; set; }
        [Required] public string zip { get; set; }
        [Required] [EmailAddress] public string email { get; set; }
        public string mobile { get; set; }
    }
}