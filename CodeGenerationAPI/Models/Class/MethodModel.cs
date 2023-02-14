using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleCodeGenerator1.Models.Class
{
    public class MethodModel
    {
        public string? Name { get; set; }
        public string? ReturnType { get; set; }
        public List<PropertyModel>? Parameters { get; set; }
    }
}
