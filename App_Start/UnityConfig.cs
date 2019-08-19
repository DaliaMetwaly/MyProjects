using SawyerSight.DAL.Repositories;
using SawyerSight.Web.DAL;
using SawyerSight.Web.DAL.Infrastructure;
using System.Web.Mvc;
using Unity;
using Unity.AspNet.Mvc;

namespace SawyerSight.Web
{
    public class UnityConfig
    {
        public static UnityContainer DefaultContainer { get; private set; }
        public static void RegisterComponents()
        {
            DefaultContainer = new UnityContainer();

            // register all your components with the container here 
            // it is NOT necessary to register your controllers 

            // e.g. container.RegisterType<ITestService, TestService>();             
            DefaultContainer.RegisterType<IClientDataService, ClientDataService>();            
            DefaultContainer.RegisterType<IUserDataService, UserDataService>();                        
            DefaultContainer.RegisterType<IStageDataService, StageDataService>();
            DefaultContainer.RegisterType<IMigrationDataService, MigrationDataService>();
            DependencyResolver.SetResolver(new UnityDependencyResolver(DefaultContainer));
        }
    }
}