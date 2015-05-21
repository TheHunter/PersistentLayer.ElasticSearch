using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.Test.Documents
{
    public class Student
        : Person
    {
        public int Code { get; set; }
    }
}
