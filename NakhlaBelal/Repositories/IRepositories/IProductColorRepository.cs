namespace NakhlaBelal.Repositories.IRepositories
{
    public interface IProductColorRepository : IRepository<Color>
    {
        void RemoveRange(IEnumerable<Color> productColors);
    }
}
