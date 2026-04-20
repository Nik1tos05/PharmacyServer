using Microsoft.EntityFrameworkCore;
using PharmacyServer.Models;

namespace PharmacyClient.Data
{
    public static class ModelBuilderExtensions
    {
        public static void ConfigureModel(this ModelBuilder modelBuilder)
        {
            // Конфигурация вызывается из OnModelCreatingPartial в серверном проекте
            // Здесь ничего добавлять не нужно, так как все отношения уже настроены
        }
    }
}
