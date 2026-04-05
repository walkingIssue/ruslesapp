namespace rustles.core.Shared
{
    public interface IFactory<T>
    {
        T Create(params object[]? init);
    }
}
