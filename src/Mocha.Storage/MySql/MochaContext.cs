using Microsoft.EntityFrameworkCore;

namespace Mocha.Storage.Mysql;

public class MochaContext : DbContext
{
    public MochaContext(DbContextOptions options) : base(options)
    {
    }
}
