using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerationAPI.Models.Class
{
    public class ClassModel
    {
        public string Name { get; set; } = string.Empty;                // Name of the class
        public List<PropertyModel> Properties { get; set; } = new();    // List of member properties
        public List<MethodModel> Methods { get; set; } = new();         // List of member methods
        public List<string>? InheritedClassesNames { get; set; }        // List of the names of the directly inherited classes
        public string? FullPackagePath { get; set; }     // The full path containing the names of all the nested packages above this class
    }
}
