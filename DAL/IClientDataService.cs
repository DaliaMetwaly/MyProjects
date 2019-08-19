using SawyerSight.Models.DAL;
using System.Collections.Generic;

namespace SawyerSight.DAL.Repositories
{
    public interface IClientDataService
    {
        Client Get(int Id);
        IEnumerable<Client> GetAll();
        void Add(Client entity);
        void Delete(Client entity);
        void Update(Client entity);
        void SaveWaterfallContext(int clientID, int datasetID, string context);
        string GetWaterfallContext(int clientID);
        string GetWaterfallContextByReportDataSetID(int reportDataSetID);
        int GetReportDataSetID(int clientID);
        int AddNewReportDataSet(int clientID);
        void AddNewOneDriveShare(int clientUniqueID, string clientID, string email, string message, string shareLink, string shareID);
        void UpdateOneDriveShareID(string shareLink, string shareID);
        IEnumerable<OneDriveInvitedUser> GetAllOneDriveShares(string ClientID);
        void DeleteOneDriveShare(string shareID);
        int checkIfClientExportData(int reportDataSetID);
    }
}
