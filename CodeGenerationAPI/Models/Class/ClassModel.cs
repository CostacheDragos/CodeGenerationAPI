﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerationAPI.Models.Class
{
    public class ClassModel
    {
        public string Name { get; set; } = string.Empty;
        public List<PropertyModel> Properties { get; set; } = new();
        public List<MethodModel> Methods { get; set; } = new();
        public List<string>? InheritedClassesNames { get; set; }
        public List<string>? ImplementedInterfacesNames { get; set; }
        public List<MethodModel>? OverriddenMethods { get; set; }
    }
}
