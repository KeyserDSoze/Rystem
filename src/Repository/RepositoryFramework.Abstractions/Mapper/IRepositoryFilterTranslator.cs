namespace RepositoryFramework
{
    public interface IRepositoryFilterTranslator
    {
        IFilterExpression Transform(SerializableFilter serializableFilter);
    }
}
