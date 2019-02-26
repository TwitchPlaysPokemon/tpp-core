namespace TPPCore.Database
{
    public class MongodbParameter : IDbParameter
    {
        public string collection;
        public string id;

        public MongoOperation operation;
    }

    public enum MongoOperation
    {
        Update,
        Insert,
        Replace,
        Delete
    }
}
