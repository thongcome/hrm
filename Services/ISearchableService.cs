namespace HRM.Services
{
    public interface ISearchableService<T> : IBaseService<T> where T : class
    {
        Task<List<T>> SearchAsync(string keyword, params string[] fields);
    }

}
