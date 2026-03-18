using Domain.Common.DbContext;

namespace Application.CreditReports
{
    public class CreditReportRepository(DatabaseSettings databaseSettings) : ICreditReportRepository
    {
        private readonly DatabaseSettings _databaseSettings = databaseSettings;
    }
}
