using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nest;

namespace PersistentLayer.ElasticSearch.Mapping
{
    public class MapResolver
    {
        // questa classe dovrà fornire i MapConfiguration definiti dall'utente 
        // NOTA:
        /*
        Nel caso della risoluzione del nome dell'identifier occorre verificare 
         * se dobbiamo fare delle modifiche sull'oggetto mapper di ElasticSearch
         * altrimenti il valore della proprietà della chiave potrebbe non essere impostato correttamente in fase di Get or Search.
        */

        private HashSet<IMapConfiguration> maps;
        private MapConfigurationComparer comparer;
        private ElasticInferrer inferrer;

        public MapResolver(ElasticInferrer inferrer)
        {
            this.comparer = new MapConfigurationComparer();
            this.maps = new HashSet<IMapConfiguration>(this.comparer);
            this.inferrer = inferrer;
        }

        public MapResolver RegisterMap(IMapConfiguration mapConfiguration)
        {
            this.maps.Add(mapConfiguration);
            return this;
        }

    }
}
