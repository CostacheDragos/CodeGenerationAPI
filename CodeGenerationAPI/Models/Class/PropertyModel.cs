using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerationAPI.Models.Class
{
    public class PropertyModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AccessModifier { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool GenerateSetter { get; set; } = false;
        public bool GenerateGetter { get; set; } = false;
        public bool IsStatic { get; set; } = false;
    }
}
