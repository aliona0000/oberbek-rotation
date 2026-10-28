using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    public class ParameterDescriptor
    {
        public string Id;          // ключ, например "mass1"
        public string DisplayName; // "Масса груза 1"
        public string Unit;        // "кг"
        public double DefaultValue;
        public double MinValue;
        public double MaxValue;
    }
}
