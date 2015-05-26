using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.Test.Documents
{
    public class Student
        : Person
    {
        protected Student()
        {
        }

        public Student(int id)
            : base(id)
        {
        }

        public int Code { get; set; }
    }
}
