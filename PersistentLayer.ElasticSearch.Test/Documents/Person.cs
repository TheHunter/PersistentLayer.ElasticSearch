using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.Test.Documents
{
    public class Person
    {
        protected Person()
        {
        }

        public Person(int id)
        {
            this.Id = id;
        }

        public int Id { get; private set; }
        public string Name { get; set; }
        public string Surname { get; set; }
    }
}
