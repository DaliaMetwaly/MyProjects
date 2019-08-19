using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Models.DAL
{
    public interface ITreeItemModel
    {
        string Id { get; set; }
        string Name { get; set; }
        string ParentId { get; }
        string ActiveFlag { get; set; }
        int Level { get; set; }
        int ReportDataSetID { get; set; }
    }
}