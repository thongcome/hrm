using HRM.Models;

namespace HRM.BusinessModel
{
    public class HRBaseSalaryFixWithSignModel : Hrbasepayrollfixed
    {

        public int? SignFlag { get; set; }

        public String? description { get; set; }


    }
}
