using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeGenerationAPI.Models.Variable;

namespace CodeGenerationAPI.Models.Class
{
    public class PropertyModel : VariableModel
    {
        public string AccessModifier { get; set; } = string.Empty;
        public bool GenerateSetter { get; set; } = false;
        public bool GenerateGetter { get; set; } = false;
        public bool IsStatic { get; set; } = false;
    }
}
