using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.Metadata
{
    public class DocOperationResponse
    {
        public string Operation { get; set; }

        public string Index { get; set; }

        public string Type { get; set; }

        public string Id { get; set; }

        public string Version { get; set; }

        public int Status { get; set; }

        public string Error { get; set; }
    }
}
