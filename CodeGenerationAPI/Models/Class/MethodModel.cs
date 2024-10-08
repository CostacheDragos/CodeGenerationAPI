﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerationAPI.Models.Class
{
    public class MethodModel
    {
        public string Name { get; set; } = string.Empty;
        public string AccessModifier { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public List<PropertyModel>? Parameters { get; set; }
    }
}
