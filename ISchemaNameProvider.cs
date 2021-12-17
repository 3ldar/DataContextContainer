namespace DataContextContainer
{
    public interface ISchemaNameProvider
    {
        public string SchemaName { get; set; }
    }
    public class SchemaNameProvider : ISchemaNameProvider
    {
        public string SchemaName { get; set; }      
    }
 
}