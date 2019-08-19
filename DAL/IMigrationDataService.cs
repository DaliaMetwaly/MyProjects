using SawyerSight.Models.DAL;
using SawyerSight.Web.Models.ETL.Costpoint;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SawyerSight.Web.DAL
{
    public interface IMigrationDataService
    {
        void ImportOrganizations(IEnumerable<ITreeItemModel> list, int reportDataSetID);
        void ImportProjects(IEnumerable<PROJ> list, int reportDataSetID);
        void CrossMapProjects(int reportDataSetID, int level);
        void ImportCustomers(IEnumerable<CUST> list, int reportDataSetID);
        void ImportGlPostSum(IEnumerable<GlPostSum> list, int reportDataSetID);
        DataTable GenerateWaterfall(string query);
        void SaveWaterfall(int clientID, int reportDataSetID, string waterfallText);
        void ImportAccounts(IEnumerable<ACCT> accounts, int reportDataSetI);
    }
}
