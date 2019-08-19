using SawyerSight.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.DAL
{
    public class AuthorizationDataService : DataService
    {        
        public AuthorizationDataService()
           : base(Config.Current.DefaultConnectionString)
        {
        }

        public UserTokenCache FirstOrDefault(string userId)
        {
            return Query<UserTokenCache>("SELECT TOP 1 * FROM [dbo].[UserTokenCaches] WHERE [webUserUniqueId]=@userId", new { userId }).FirstOrDefault();
        }

        public void Delete(string userId)
        {
            Execute("DELETE FROM [dbo].[UserTokenCaches] WHERE [webUserUniqueId]=@userId", new { userId });
        }

        public void AddOrUpdate(UserTokenCache entity)
        {
            string sql = @"
UPDATE [dbo].[UserTokenCaches] SET cacheBits=@cacheBits, LastWrite=@lastWrite 
WHERE [webUserUniqueId]=@webUserUniqueId

IF @@ROWCOUNT = 0
BEGIN
   INSERT INTO [dbo].[UserTokenCaches] (cacheBits, LastWrite, webUserUniqueId) VALUES(@cacheBits, @lastWrite, @webUserUniqueId);
END";
            Execute(sql, new
            {
                webUserUniqueId = entity.webUserUniqueId,
                cacheBits = entity.cacheBits,
                entity.LastWrite
            });
        }
    }
}