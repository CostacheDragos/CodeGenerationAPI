using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleCodeGenerator1.Models.Class
{
    public class ClassModel
    {
        public string? Name { get; set; }
        public List<PropertyModel>? Properties { get; set; }
        public List<MethodModel>? Methods { get; set; }
    }
}
