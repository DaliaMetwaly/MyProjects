using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Security;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using SawyerSight.Web.DAL;

namespace SawyerSight.Web.Models
{
    public class ADALTokenCache : TokenCache
    {
        private string userId;
        private UserTokenCache Cache;
        private AuthorizationDataService authorizationDataService = new AuthorizationDataService();
        
        public ADALTokenCache(string signedInUserId)
        {            
            // associate the cache to the current user of the web app
            userId = signedInUserId;
            this.AfterAccess = AfterAccessNotification;
            this.BeforeAccess = BeforeAccessNotification;
            this.BeforeWrite = BeforeWriteNotification;
            // look up the entry in the database            

            Cache = authorizationDataService.FirstOrDefault(userId);
            
            // place the entry in memory
            this.Deserialize((Cache == null) ? null : MachineKey.Unprotect(Cache.cacheBits, "ADALCache"));            
        }

        // clean up the database
        public override void Clear()
        {
            base.Clear();
            authorizationDataService.Delete(userId);                       
        }

        // Notification raised before ADAL accesses the cache.
        // This is your chance to update the in-memory copy from the DB, if the in-memory version is stale
        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            
            if (Cache == null)
            {

                // first time access
                Cache = authorizationDataService.FirstOrDefault(userId);
            }
            else
            {
                var status = authorizationDataService.FirstOrDefault(userId);
                if (status.LastWrite > Cache.LastWrite)
                {
                    Cache = status;
                }                
            }            

            this.Deserialize((Cache == null) ? null : MachineKey.Unprotect(Cache.cacheBits, "ADALCache"));            
        }

        // Notification raised after ADAL accessed the cache.
        // If the HasStateChanged flag is set, ADAL changed the content of the cache
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if state changed
            if (this.HasStateChanged)
            {
                if (Cache == null)
                {
                    Cache = new UserTokenCache
                    {
                        webUserUniqueId = userId
                    };
                }

                Cache.cacheBits = MachineKey.Protect(this.Serialize(), "ADALCache");
                Cache.LastWrite = DateTime.Now;

                authorizationDataService.AddOrUpdate(Cache);
                
                HasStateChanged = false;
            }
        }

        void BeforeWriteNotification(TokenCacheNotificationArgs args)
        {
            // if you want to ensure that no concurrent write take place, use this notification to place a lock on the entry
        }

        

        public override void DeleteItem(TokenCacheItem item)
        {
            base.DeleteItem(item);
        }
    }
}
