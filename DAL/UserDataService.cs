using Dapper;
using SawyerSight.DAL.Repositories;
using SawyerSight.Models.DAL;
using SawyerSight.Models.ViewModel;
using SawyerSight.Web.Models.DAL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SawyerSight.Web.DAL
{
    public class UserDataService : DataService, IUserDataService
    {
        public UserDataService()
            : base(Config.Current.DefaultConnectionString)
        {
        }

        public void AddUser(User entity)
        {
            const string sql =
            @"INSERT INTO [SawyerSight].[Users]
                ([UPN]
                ,[DisplayName]
                ,[ClientID])
                VALUES
                (@UPN
                ,@DisplayName
                ,@ClientID)";

            Execute(sql, new
            {
                UPN = entity.UPN,
                DisplayName = entity.DisplayName,
                ClientID = entity.ClientID
            });
        }

        public void AddUser(ManageUserVM user)
        {
            AddUser(new User() { UPN = user.UPN, DisplayName = user.DisplayName });
            AddRoleToUser(new UserRoles() { UPN = user.UPN, RoleID = user.UserRoles.ID});
            if(user.UserClients!=null)
            {
                foreach (Client client in user.UserClients)
                {
                    AddClientToUser(user.UPN, client.UniqueID);
                }
            }
           
        }

        public IEnumerable<string> GetRoles(string upn)
        {
            const string sql =
            @"SELECT RoleName FROM SawyerSight.Roles r 
              JOIN SawyerSight.UserRoles ur on r.Id = ur.RoleID
              WHERE ur.UPN = @UPN; ";

            return Query<string>(sql, new { @UPN = upn });
        }

        public IEnumerable<Roles> GetAllRoles()
        {
            const string sql =
            @"SELECT ID,RoleName FROM SawyerSight.Roles";
            return Query<Roles>(sql);
        }

        public User GetUser(string upn)
        {
            return Query<User>("SELECT TOP 1 [UPN],[DisplayName],[ClientID] FROM SawyerSight.Users WHERE UPN=@UPN", new { @UPN = upn }).FirstOrDefault();
        }

        public Roles GetUserRoles(string upn)
        {
            const string sql =
                @"SELECT Id,RoleName FROM SawyerSight.Roles r 
                JOIN SawyerSight.UserRoles ur 
                on r.Id = ur.RoleID
                WHERE ur.UPN = @UPN; ";

            return Query<Roles>(sql, new { @UPN = upn }).FirstOrDefault();
        }

        public IEnumerable<Client> GetUserClients(string upn)
        {
            const string sql =
                @"SELECT UniqueID,ClientName FROM SawyerSight.Clients c 
                JOIN SawyerSight.UserClients uc 
                on c.UniqueID = uc.ClientID
                WHERE uc.UPN = @UPN; ";

            return Query<Client>(sql, new { @UPN = upn });
        }
        
        public IEnumerable<User> GetAll()
        {
            return Query<User>("SELECT * FROM SawyerSight.Users");
        }

        public void AddRoleToUser(UserRoles userRole)
        {
            const string sql =
                @"DELETE FROM [SawyerSight].[UserRoles]
                  WHERE UPN=@UPN
                 INSERT INTO [SawyerSight].[UserRoles](UPN,RoleID) VALUES(@UPN,@RoleID)";

            Execute(sql, new
            {
                UPN = userRole.UPN,
                RoleID = userRole.RoleID,
               
            });
        }

        public void DeleteUserClients(string upn)
        {
            const string sql =
                @"DELETE FROM [SawyerSight].[UserClients]
                  WHERE UPN=@UPN
                ";

            Execute(sql, new
            {
                UPN = upn,
            });
        }

        public void AddClientToUser(string upn,int clientID)
        {
            const string sql =
                @"INSERT INTO [SawyerSight].[UserClients](UPN,ClientID) VALUES(@UPN,@ClientID)";

            Execute(sql, new
            {
                UPN = upn,
                ClientID = clientID,

            });
        }

        public void DeleteUser(string upn)
        {
            const string sql =
                @"DELETE FROM [SawyerSight].[UserRoles]   WHERE UPN=@UPN
                  DELETE FROM [SawyerSight].[UserClients] WHERE UPN=@UPN
                  DELETE FROM [SawyerSight].[Users]       WHERE UPN=@UPN";

            Execute(sql, new
            {
                UPN = upn,
            });
        }

        public List<ManageUserVM> GetAllManageUsers()
        {
            List<ManageUserVM> manageUsers=new List<ManageUserVM>() ;
            IEnumerable<User> allUsers = GetAll();
            foreach(User user in allUsers)
            {
                Roles roles = GetUserRoles(user.UPN);
                Roles assignedRole = roles != null ? roles : new Roles();
                manageUsers.Add( new ManageUserVM() { UPN = user.UPN, DisplayName = user.DisplayName,  UserRoles = assignedRole, UserClients = GetUserClients(user.UPN) });
            }

            return manageUsers;
        }

        public void UpdateManageUsers(ManageUserVM user)
        {
            UserRoles userRole = new UserRoles() { UPN=user.UPN, RoleID= user.UserRoles.ID };
            AddRoleToUser(userRole);
            DeleteUserClients(user.UPN);
            if (user.UserClients!=null)
            {
                if (user.UserClients.Count()>0)
                {
                     foreach (Client client in user.UserClients)
                     {
                         AddClientToUser(user.UPN, client.UniqueID);
                     }
                }
            }

        }

        
    }
}