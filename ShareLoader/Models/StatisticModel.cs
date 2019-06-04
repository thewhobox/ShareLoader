using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ShareLoader.Models
{
    public class StatisticModel
    {
        public int ID { get; set; }
        [MaxLength(25)]
        public string EntityType { get; set; }
        public int EntityID { get; set; }
        public double Value { get; set; }
        public DateTime Stamp { get; set; }
        public SourceType Source { get; set; }
        
        public StatisticModel()
        {
            Stamp = DateTime.Now;
        }

        public enum SourceType
        {
             Item,
             Group,
             Account
        }
    }
}
