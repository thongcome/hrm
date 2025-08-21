namespace HRM.Services.Payroll
{
    using HRM.Data;
    using HRM.Models;
    using Microsoft.EntityFrameworkCore;

    public class PayrollCalculationService
    {
        private readonly IDbContextFactory<HRMContext> _dbFactory;

        public PayrollCalculationService(IDbContextFactory<HRMContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        // สร้างคลาสสำหรับผลลัพธ์
        public class PayrollCalculationResult
        {
            public List<Hrpayroll> Payrolls { get; set; } = new();
            public List<Hrpayrolldet> PayrollDetails { get; set; } = new();
        }

        public async Task<PayrollCalculationResult> ProcessPayrollAsync(List<Hremployee> employees, string period)
        {
            var result = new PayrollCalculationResult();
            var context =  _dbFactory.CreateDbContext();
            Hrucfsecurity? socailContant = await context.Hrucfsecuritys.Where(x => x.SecurityCode == "01").FirstOrDefaultAsync();

            // ... (ย้าย Logic ทั้งหมดจาก CalculatePayroll() มาไว้ที่นี่) ...
            foreach (var item in employees)
            {
                // Logic การคำนวณของคุณทั้งหมด...
                // ...
                // เมื่อได้ผลลัพธ์ ก็ Add เข้าไปใน result.Payrolls และ result.PayrollDetails
            }

            return result;
        }
    }
}
