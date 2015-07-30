namespace PersistentLayer.ElasticSearch.Test.Documents
{
    public sealed class MySealedClass
        : Student
    {
        public MySealedClass()
        {
        }

        public MySealedClass(int id)
            : base(id)
        {
        }

        public int? MuCustomProp { get; set; }

        public string Mcs { get; set; }
    }
}
