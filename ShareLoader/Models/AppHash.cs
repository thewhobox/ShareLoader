using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ShareLoader.Models
{
    public class AppHash
    {
        public int Id { get; set; }
        [MaxLength(16)]
        public string Secret { get; set; }
    }
}
