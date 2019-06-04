using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ShareLoader.Models
{
    public class AccountModel
    {
        public int ID { get; set; }
        [MaxLength(20)]
        public string Name { get; set; }
        [MaxLength(50)]
        public string Username { get; set; }
        [MaxLength(50)]
        public string Password { get; set; }
        public float TrafficLeft { get; set; }
        public float TrafficLeftWeek { get; set; }
        public bool IsPremium { get; set; }
        public DateTime ValidTill { get; set; }
        public float Credit { get; set; }
        [MaxLength(2)]
        public string Hoster { get; set; }
    }
}
