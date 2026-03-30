using NakhlaBelal.DataAccess;
using NakhlaBelal.Repositories.IRepositories;

namespace NakhlaBelal.Repositories
{
    public class ProductColorRepository : Repository<Color>, IProductColorRepository
    {
        private ApplicationDbContext _context;// = new();

        public ProductColorRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public void RemoveRange(IEnumerable<Color> productColors)
        {
            _context.RemoveRange(productColors);
        }
    }
}
