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
