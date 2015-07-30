namespace PersistentLayer.ElasticSearch.Test.Documents
{
    public class Person
    {
        public Person()
        {
        }

        public Person(int id)
        {
            this.Id = id;
        }

        public int? Id { get; private set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Cf { get; set; }
    }
}
