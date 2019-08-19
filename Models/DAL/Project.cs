namespace SawyerSight.Models.DAL
{
    public class Project:ITreeItemModel
    {       
        public string OrgId { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string ParentId
        {
            get
            {
                if (Id.LastIndexOf('.') < 1) return null;
                return Id.Substring(0, Id.LastIndexOf('.'));
            }
        }
        public string ActiveFlag { get; set; }
        public int Level { get; set; }
        public int ReportDataSetID { get; set; }
        
    }
}