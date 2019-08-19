using SawyerSight.DAL.Repositories;
using SawyerSight.Models.DAL;
using System.Collections.Generic;
using System.Linq;

namespace SawyerSight.Web.DAL
{

    public class ClientDataService : DataService, IClientDataService
    {
        public ClientDataService()
            : base(Config.Current.DefaultConnectionString)
        {
        }

        public void Add(Client entity)
        {
            const string sql = @"INSERT INTO [SawyerSight].[Clients]
           ([ClientID]
           ,[ClientName]
           ,[PrimaryContact]
           ,[ContactEmail]
           ,[ContactAddress1]
           ,[ContactAddress2]
           ,[PhoneNumber]
           ,[City]
           ,[State]
           ,[PostalCode])
     VALUES
           (@ClientID
           ,@ClientName
           ,@PrimaryContact
           ,@ContactEmail
           ,@ContactAddress1
           ,@ContactAddress2
           ,@PhoneNumber
           ,@City
           ,@State
           ,@PostalCode)";

            Execute(sql, new
            {
                ClientID = entity.ClientID,
                ClientName = entity.ClientName,
                PrimaryContact = entity.PrimaryContact,
                ContactEmail = entity.ContactEmail,
                ContactAddress1 = entity.ContactAddress1,
                ContactAddress2 = entity.ContactAddress2,
                PhoneNumber = entity.PhoneNumber,
                City = entity.City,
                State = entity.State,
                PostalCode = entity.PostalCode
            });
        }

        public void SaveWaterfallContext(int clientID, int datasetID, string context)
        {
            Execute("UPDATE [SawyerSight].[ReportDataSet] SET Context=@context WHERE ClientUniqueID=@clientID AND DataSetID=@reportDataSetID", new { @context = context, @clientID = clientID, @reportDataSetID = datasetID });
        }

        public string GetWaterfallContext(int clientID)
        {
            return Query<string>("SELECT Context FROM [SawyerSight].[ReportDataSet] WHERE ClientUniqueID=@clientID", new { @clientID = clientID }).FirstOrDefault();
        }

        public string GetWaterfallContextByReportDataSetID(int reportDataSetID)
        {
            return Query<string>("SELECT Context FROM [SawyerSight].[ReportDataSet] WHERE DataSetID=@reportDataSetID", new { @reportDataSetID = reportDataSetID }).FirstOrDefault();

        }
        public void Delete(Client entity)
        {
            Execute("DELETE FROM SawyerSight.Clients WHERE UNIQUEID=@UniqueID", new { UniqueID = entity.UniqueID });
        }

        public Client Get(int Id)
        {
            return Query<Client>("SELECT * FROM SawyerSight.Clients WHERE UniqueID=@ClientID", new { ClientID = Id }).FirstOrDefault();
        }

        public IEnumerable<Client> GetAll()
        {            
            if (CurrentUser.IsAdmin)
            {
                return Query<Client>("SELECT * FROM SawyerSight.Clients");
            }
            else
            {
                string upn = CurrentUser.Upn();
                return Query<Client>("SELECT c.* FROM SawyerSight.Clients c INNER JOIN SawyerSight.UserClients uc ON c.UniqueID = uc.ClientID WHERE uc.UPN=@UPN", new { @UPN = upn });
            }            
        }

        public void Update(Client entity)
        {
            const string sql =
@"UPDATE [SawyerSight].[Clients]
SET [ClientID] = @ClientID
    ,[ClientName] = @ClientName
    ,[PrimaryContact] = @PrimaryContact
    ,[ContactEmail] = @ContactEmail
    ,[ContactAddress1] = @ContactAddress1
    ,[ContactAddress2] = @ContactAddress2
    ,[PhoneNumber] = @PhoneNumber
    ,[City] = @City
    ,[State] = @State
    ,[PostalCode] = @PostalCode
WHERE UniqueID=@UniqueID;";

            Execute(sql, new
            {
                ClientID = entity.ClientID,
                ClientName = entity.ClientName,
                PrimaryContact = entity.PrimaryContact,
                ContactEmail = entity.ContactEmail,
                ContactAddress1 = entity.ContactAddress1,
                ContactAddress2 = entity.ContactAddress2,
                PhoneNumber = entity.PhoneNumber,
                City = entity.City,
                State = entity.State,
                PostalCode = entity.PostalCode,
                UniqueID = entity.UniqueID
            });
        }

        public int GetReportDataSetID(int clientID)
        {
            return Query<int>("SELECT DataSetID FROM [SawyerSight].[ReportDataSet] WHERE ClientUniqueID=@clientID", new { @clientID = clientID }).FirstOrDefault();
        }

        public int AddNewReportDataSet(int clientID)
        {
            return Query<int>(@"INSERT INTO [SawyerSight].[ReportDataSet]
                           ([ClientUniqueID]
                           ,[DateAdded]
                           ,[IsActive]
                           ,[ReportID]
                           ,[Context])
                     VALUES
                           (@clientID
                           , GETDATE()
                           , 1
                           , 1
                           , '');
                            SELECT SCOPE_IDENTITY();", new { @clientID = clientID }).Single();
        }

        public void AddNewOneDriveShare(int clientUniqueID, string clientID, string email, string message, string shareLink, string shareID)
        {
            string sql = $@"INSERT INTO [SawyerSight].[OneDriveShares]
                                       ([ClientUniqueID]
                                       ,[ClientID]
                                       ,[Email]
                                       ,[ShareLink]
                                       ,[Message]
                                       ,[ShareID])
                                 VALUES
                                       (@ClientUniqueID
                                       ,@ClientID
                                       ,@Email
                                       ,@ShareLink
                                       ,@Message
                                       ,@ShareID)";
            Execute(sql, new { @ClientUniqueID = clientUniqueID, @ClientID = clientID, @Email = email, @ShareLink = shareLink, @Message = message, @ShareID = shareID });
        }

        public IEnumerable<OneDriveInvitedUser> GetAllOneDriveShares(string ClientID)
        {
            string sql = $@"SELECT *
                        FROM [SawyerSight].[OneDriveShares] WHERE ClientID=@clientID";
            return Query<OneDriveInvitedUser>(sql, new { @clientID = ClientID });
        }

        public void UpdateOneDriveShareID(string shareLink, string shareID)
        {
            string sql = $@"UPDATE [SawyerSight].[OneDriveShares] SET ShareID=@ShareID WHERE ShareLink=@ShareLink";
            Execute(sql, new { @ShareID = shareID, @ShareLink=shareLink});
        }

        public void DeleteOneDriveShare(string shareID)
        {
            string sql = $@"DELETE FROM [SawyerSight].[OneDriveShares] WHERE ShareID=@ShareID";
            Execute(sql, new { @ShareID = shareID });
        }

        public int checkIfClientExportData(int reportDataSetID)
        {
            return Query<int>("select count(*) from [SawyerSight].[GL_POST_SUM] where [ReportDataSetID]=@reportDataSetID", new { @reportDataSetID = reportDataSetID }).FirstOrDefault();
        }

    }
}