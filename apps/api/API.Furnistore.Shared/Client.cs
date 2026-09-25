using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Furnistore.Shared
{
    public class Client
    {
        public int ID { get; set; }

        public string? UserId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public DateTime BirthDate { get; set; }

        public string Phone { get; set; }

        public string Address { get; set; }
    }
}