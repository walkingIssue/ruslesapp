using rustles.core.Systems.ResolutionQue;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
/*
0.  objektidentitet                     | String
1.  objektversion                       | Integer64
2.  versiongiltigfran                   | DateTime
3.  versiongiltigtill                   | DateTime
4.  objekttyp                           | String
5.  enhetstyp                           | String
6.  gransstatus                         | String
7.  lanskod                             | String
8.  kommunkod                           | String
9.  landskod                            | String
10. hogstaforvaltningsniva              | String
11. beslutatnamn                        | String
12. namnkortform                        | String
13. landareal_varde                     | Integer64
14. landareal_enhet                     | String
15. vattenareal_varde                   | Integer64
16. vattenareal_enhet                   | String
17. totalareal_varde                    | Integer64
18. totalareal_enhet                    | String
19. referensdatumfran                   | Date
20. referensdatumtill                   | Date 
 */
namespace rustles.core.Systems.GDAL.Abstractions
{
    public interface IGeoRasterAccess
    {
        /// <summary>
        /// Lightning fast lookup of a position
        /// Preserves callers order of objects in return
        /// expect the indexes in the return to tell the munipId of the object in the tileLocalizedIds
        /// </summary>
        /// <param name="tileKey"></param>
        /// <param name="tileLocalizedIds"></param>
        /// <returns></returns>
        public int ResolvePosition(TileKey tileKey, ReadOnlySpan<ushort> tileLocalizedIds, Span<int> result);
    }

    public class GeoRasterAccess : IGeoRasterAccess
    {
        private object _lock = new object();
        private OSGeo.GDAL.Dataset _dataset;
        public GeoRasterAccess(OSGeo.GDAL.Dataset dataset)
        {
            _dataset = dataset;
        }
        public int ResolvePosition(TileKey tileKey, ReadOnlySpan<ushort> tileLocalizedIds, Span<int> result)
        {
            int[] buffer = new int[256 * 256];
            _dataset
                .GetRasterBand(1)
                .ReadRaster(tileKey.RasterIdY * 256, tileKey.RasterIdX * 256, 256, 256, buffer, 256, 256, 0, 0);

            if(result.Length < 1)
                throw new ArgumentException("Result too short.", nameof(result));

            for(int i = 0; i < tileLocalizedIds.Length; i++)
            {
                result[i] = buffer[tileLocalizedIds[i]];
            }
            return buffer.Length;
        }
    }
}
