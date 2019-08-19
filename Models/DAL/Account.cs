using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Models.DAL
{
    public class Account : ITreeItemModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ActiveFlag { get; set; }
        public int ReportDataSetID { get; set; }
        public int IsActualAccount { get; set; }
        public int Level { get; set; }
        public string ParentId
        {
            get
            {
                if (Id.LastIndexOf('-') < 1) return null;
                return Id.Substring(0, Id.LastIndexOf('-'));
            }
        }
    }

    public class AccountStartWith
    {
        public string Id { get; set; }
        public string Name
        {
            get
            {
                return $"{Id} ({NumAccounts})";
            }
        }
        public int NumAccounts { get; set; }
    }
}