﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BExIS.Security.Entities.Objects;

namespace BExIS.Security.Entities.Security
{
    public class FeaturePermission : Permission
    {
        public virtual Feature Feature { get; set; }
    }
}