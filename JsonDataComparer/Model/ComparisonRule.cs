using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonDataComparer.Model
{
    public class ComparisonRule
    {        
        public string Name { get; set; }
        public Dictionary<string, ComparisonRule> ChildRules { get; set; } = new Dictionary<string, ComparisonRule>();
    }
}
