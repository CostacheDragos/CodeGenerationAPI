using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleCodeGenerator1.Models.Class
{
    public class PropertyModel
    {
        public string? Name { get; set; }
        public string? AccessModifier { get; set; }
        public string? Type { get; set; }
        public bool GenerateSetter { get; set; }
        public bool GenerateGetter { get; set; }
    }
}
