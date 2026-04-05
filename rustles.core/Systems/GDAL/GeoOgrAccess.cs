using MaxRev.Gdal.Core;

using OSGeo.OGR;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using rustles.core.Entities.DTOs;
using rustles.core.Shared;
using rustles.core.Systems.GDAL.Abstractions;

namespace rustles.core.Systems.GDAL
{
    public class GeoOgrAccess : IGeoOgrAccess
    {
        OSGeo.OGR.DataSource _source;
        Dictionary<string, int> fields = [];
        List<Municipality> munips = [];
        IFactory<Municipality> _munipFact;
        public GeoOgrAccess(OSGeo.OGR.DataSource source, IFactory<Municipality> factory)
        {
            _source = source;
            _munipFact = factory;
            InitFields();
        }
        private void InitFields()
        {
            fields.Add("munipname", 12);
            fields.Add("countycode", 7);
            fields.Add("munipcode", 8);
            fields.Add("countrycode", 9);
        }
        public IEnumerable<Municipality> GetMunicipalities(OgrAccessOptions? opts = null)
        {
            //check for the generated json abstraction

            //go into the layer
            var layer = _source.GetLayerByName("kommun");
            layer.ResetReading();
            Feature f;
            while((f = layer.GetNextFeature()) != null)
            {
                munips.Add(CreateMunip(ref f));
            }
            return munips;
        }
        
        Municipality CreateMunip(ref OSGeo.OGR.Feature feature)
        {
            return _munipFact.Create(
            [feature.GetFieldAsString(fields["munipname"]),
                    ParseId(feature.GetFieldAsString(fields["munipcode"])),
                    ParseId(feature.GetFieldAsString(fields["countycode"])),
                    ParseId(feature.GetFieldAsString(fields["countrycode"]))
            ]);
        }
        int ParseId(string parse)
        {
            int result = -1;
            int.TryParse(parse, out result);
            return result;
        }
        public Task<IEnumerable<Municipality>> GetMunicipalitiesAsync(OgrAccessOptions? opts = null)
        {
            return Task.Run(() => { return GetMunicipalities(opts); });
        }
        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        /// <param name="opts"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void SerializeMunicipaliteis(OgrAccessOptions? opts = null)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        /// <param name="opts"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task SerializeMunicipalities(OgrAccessOptions? opts = null)
        {
            throw new NotImplementedException();
        }
    }

}
