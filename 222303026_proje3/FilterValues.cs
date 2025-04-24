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
                static int filterSize = 1, intensity = 1, threshold = 1;
                public static int FilterSize { get; set; }
                public static int Intensity { get; set; }
                public static int Threshold { get; set; }
            }
            public static class OilPaintFilterValues
            {
                static int filterSize = 1, intensity = 1, threshold = 1;
                public static int FilterSize { get; set; }
                public static int Intensity { get; set; }
                public static int Threshold { get; set; }
            }
        }
    }
}
