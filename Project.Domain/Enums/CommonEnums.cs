using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Domain.Enums
{

    public enum EmploymentStatus
    {
        Active,
        Terminated,
        Suspended,
        OnLeave,
        Probation
    }

    public enum EmployeeType
    {
        FullTime,
        PartTime,
        Contract,
        Intern,
        Temporary
    }

    public enum AttendanceStatus
    {
        Present,
        Absent,
        HalfDay,
        OnLeave,
        Holiday,
        Weekend,
        LateComing,
        EarlyLeaving
    }

    public enum LeaveStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }

    public enum PayrollStatus
    {
        Draft,
        Processing,
        Completed,
        Approved,
        Paid
    }

    public enum ShiftType
    {
        Morning,
        Evening,
        Night,
        General,
        Rotating,
        Flexible
    }

    public enum UserRole
    {
        SuperAdmin,
        HRAdmin,
        Manager,
        Employee,
        PayrollOfficer,
        AttendanceOfficer
    }

    public enum ClockMethod
    {
        Biometric,
        Mobile,
        Web,
        RFID,
        Manual,
        QRCode
    }

    public enum OvertimeType
    {
        Regular,
        Weekend,
        Holiday,
        Night
    }

    public enum ComponentType
    {
        Earning,
        Deduction
    }

    public enum CalculationMethod
    {
        Fixed,
        Percentage,
        Formula,
        HourlyRate
    }

    public enum ApprovalStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
