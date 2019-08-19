using SawyerSight.Models.DAL;
using SawyerSight.Models.ViewModel;
using SawyerSight.Web.Models.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SawyerSight.DAL.Repositories
{
    public interface IUserDataService
    {
        void AddUser(User entity);
        void AddUser(ManageUserVM user);
        User GetUser(string upn);
        IEnumerable<string> GetRoles(string upn);
        IEnumerable<Roles> GetAllRoles();
        Roles GetUserRoles(string upn);
        IEnumerable<Client> GetUserClients(string upn);
        IEnumerable<User> GetAll();
        List<ManageUserVM> GetAllManageUsers();
        void UpdateManageUsers(ManageUserVM user);
        void DeleteUser(string UPN);

    }
}
