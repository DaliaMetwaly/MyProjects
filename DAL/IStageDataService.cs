using SawyerSight.Models.DAL;
using SawyerSight.Web.Models.ETL.Costpoint;
using System.Collections.Generic;
using System.Data;

namespace SawyerSight.Web.DAL
{
    public interface IStageDataService
    {
        IEnumerable<Project> GetProjects(int reportDataSetID);
        IEnumerable<PROJ> GetETLProjects(int reportDataSetID);
        IEnumerable<Organization> GetOrganizations(int reportDataSetID);
        IEnumerable<FiscalYear> GetFiscalYears(int reportDataSetID);
        IEnumerable<Account> GetAllAccounts(int reportDataSetID);
        IEnumerable<Account> GetRevenueAccounts(int reportDataSetID);
        IEnumerable<Account> GetCostsAccounts(int reportDataSetID);
        void InsertTable(string tableName, DataTable table, int reportDataSetID);
        IEnumerable<GlPostSum> GetAllTrialBalanceTransactionAmounts(IEnumerable<string> selectedProjects, IEnumerable<string> selectedAccounts, int reportDataSetID);
        IEnumerable<CUST> GetAllCustomers(int reportDataSetID);
        IEnumerable<ACCT> GetAllAcct(int reportDataSetID, IEnumerable<string> ACCTID);
    }
}
