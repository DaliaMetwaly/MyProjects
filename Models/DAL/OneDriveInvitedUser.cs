using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Models.DAL
{
    public class OneDriveInvitedUser
    {
        public int ID { get; set; }
        public int ClientUniqueID { get; set; }
        public string ClientID { get; set; }
        public string Email { get; set; }
        public string ShareLink { get; set; }
        public string Message { get; set; }
        public string ShareID { get; set; }
    }
}