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
        public List<ConstructorModel>? Constructors { get; set; }       // List of constructors that need to be generated
        
        public bool GenerateDestructor { get; set; } = false;
        public DestructorModel? Destructor { get; set; }


        public List<PropertyModel> Properties { get; set; } = new();    // List of member properties
        public List<MethodModel> Methods { get; set; } = new();         // List of member methods
        public List<Inheritance>? InheritedClasses { get; set; }        // List of the names of the directly inherited classes
        public string? FullPackagePath { get; set; }     // The full path containing the names of all the nested packages above this class
    }

    public class Inheritance
    {
        public string Name { get; set; } = string.Empty;
        public string AccessSpecifier { get; set; } = string.Empty;
    }

}
