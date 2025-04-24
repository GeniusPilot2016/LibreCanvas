using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _222303026_proje3
{
    public class FilterValues
    {
        public static class BasicFiltersValues
        {
             
        }
        public static class ArtisticFiltersValues
        {
            public static class CartoonFilterValues
            {
                static int filterSize = 10, intensity = 5, threshold = 50;
                public static int FilterSize { get; set; } = filterSize;
                public static int Intensity { get; set; } = intensity;
                public static int Threshold { get; set; } = threshold;
            }
            public static class OilPaintFilterValues
            {
                static int filterSize = 10, intensity = 5, threshold = 50;
                public static int FilterSize { get; set; } = filterSize;
                public static int Intensity { get; set; } = intensity;
                public static int Threshold { get; set; } = threshold;
            }
        }
    }
}
