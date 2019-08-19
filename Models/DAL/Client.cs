using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SawyerSight.Models.DAL
{
    public class Client
    {
        public int UniqueID { get; set; }
        public string ClientID { get; set; }
        public string ClientName { get; set; }
        public string PrimaryContact { get; set; }
        public string ContactEmail { get; set; }
        public string ContactAddress1 { get; set; }
        public string ContactAddress2 { get; set; }
        public string PhoneNumber { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
    }
}
