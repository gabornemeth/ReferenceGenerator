﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReferenceGenerator.Engine
{
    public class PackageWithReference : Package
    {
        public PackageWithReference(string id, string version, Reference reference) : base(id, version)
        {
            Reference = reference;
        }

        public Reference Reference { get; private set; }
    }
}