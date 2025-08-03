// ===== Shared/Models/Enums/RecordType.cs =====
namespace Shared.Models.Enums
{
    public enum RecordType
    {
        CheckIn = 1,
        CheckOut = 2,
        Break = 3,
        Return = 4,
        Overtime = 5
    }

    public enum EmployeeStatus
    {
        CheckedOut = 0,
        CheckedIn = 1,
        OnBreak = 2,
        Overtime = 3
    }

    public enum VacationStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Cancelled = 3
    }

    public enum LicenseType
    {
        Basic = 1,
        Professional = 2,
        Enterprise = 3
    }

    public enum UserRole
    {
        Employee = 1,
        Supervisor = 2,
        CompanyAdmin = 3,
        SphereAdmin = 4
    }
}