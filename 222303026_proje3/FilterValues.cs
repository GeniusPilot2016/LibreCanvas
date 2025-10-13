// ArtFusion - The AI-enabled simple image editor for everyone, born as a school project by GeniusPilot2016
// Copyright (C) 2025 GeniusPilot2016
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

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
                static int filterSize = Settings1.Default.DefaultCartoonFilterSize, 
                    intensity = Settings1.Default.DefaultCartoonFilterIntensity, 
                    threshold = Settings1.Default.DefaultCartoonFilterThreshold;
                public static int FilterSize { get; set; } = filterSize;
                public static int Intensity { get; set; } = intensity;
                public static int Threshold { get; set; } = threshold;
            }
            public static class OilPaintFilterValues
            {
                static int filterSize = Settings1.Default.DefaultOilPaintFilterSize, 
                    intensity = Settings1.Default.DefaultOilPaintFilterIntensity, 
                    threshold = Settings1.Default.DefaultOilPaintFilterThreshold;
                public static int FilterSize { get; set; } = filterSize;
                public static int Intensity { get; set; } = intensity;
                public static int Threshold { get; set; } = threshold;
            }
        }
    }
}
