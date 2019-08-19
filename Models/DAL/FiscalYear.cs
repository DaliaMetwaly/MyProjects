namespace SawyerSight.Models.DAL
{
    public class FiscalYear
    {
        public string YearCode
        {
            get
            {
                if (MonthPart=="0")
                {
                    return YearPart;
                }
                return $"{YearPart}/{MonthPart}";
            }
        }        
        public string YearPart { get; set; }
        public string MonthPart { get; set; }
        public string Id
        {
            get
            {                
                if(YearCode.IndexOf("/") == -1)
                {
                    return YearCode;
                }

                return (int.TryParse(YearCode.Split('/')[1], out int code) && code > 0 && code <=12) ? Months[code-1].ToUpper() : YearCode;
            }
        }

        private string[] Months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        public string ParentId
        {
            get
            {
                if (MonthPart=="0")
                {
                    return null;
                }
                return YearPart;
            }
        }
        public string Status { get; set; }
        public string Description { get; set; }        
        public int ReportDataSetID { get; set; }
    }
}