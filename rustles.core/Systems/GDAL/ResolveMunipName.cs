using System.Collections;

using rustles.core.Entities.DTOs;
using rustles.core.Systems.GDAL.Abstractions;
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

namespace rustles.core.Systems.GDAL
{
    public class ResolveMunip : IResolveMunip
    {
        Municipality[] _munips;
        IGeoOgrAccess _ogrAccess;
        public ResolveMunip(IGeoOgrAccess ogrAccess)
        {
            _ogrAccess = ogrAccess;
            _munips = ogrAccess.GetMunicipalities().ToArray();
            Array.Sort(_munips, new MunipComparer());
        }
        public Municipality GetMunicipality(int Id)
        {
            var ph = new Municipality(null, null, null, null);
            if (Id < 0)
                return ph;

            var index = Array.BinarySearch(_munips, new Municipality(null, Id, null, null), new MunipComparer());

            if (index < 0)
                return ph;

            return _munips[index];
        }

        public Task<Municipality> GetMunicipalityAsync(int Id)
        {
            return Task.Run(() => { return GetMunicipality(Id); });
        }

        public int GetMunipId(string MunipName)
        {
            throw new NotImplementedException();
        }

        public string[] GetMunipNaturalName(int Id)
        {
            throw new NotImplementedException();
        }
    }
    public class MunipComparer : IComparer
    {
        public int Compare(object? x, object? y)
        {
            if (x is null && y is null)
                return 0;
            if (x is null && y is not null)
                return -1;
            if (x is not null && y is null)
                return 1;

            var cX = (x as Municipality);
            var cY = (y as Municipality);

            return cX.munipCode ?? 0 - cY.munipCode ?? 0;
        }
    }
}
