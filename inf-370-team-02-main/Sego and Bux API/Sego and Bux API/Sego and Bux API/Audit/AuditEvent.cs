namespace Sego_and__Bux.Audit
{
    public static class AuditEvent
    {
        public static class Auth
        {
            public const string Register = "Auth/Register";
            public const string VerifyOtp = "Auth/VerifyOtp";
            public const string ResendOtp = "Auth/ResendOtp";
            public const string Login = "Auth/Login";
            public const string Refresh = "Auth/Refresh";
            public const string ForgotPassword = "Auth/ForgotPassword";
            public const string ResetPassword = "Auth/ResetPassword";
        }

        public static class Account
        {
            public const string UpdateProfile = "Account/UpdateProfile";
            public const string UpdatePassword = "Account/UpdatePassword";
            public const string Delete = "Account/Delete";
        }

        public static class Employee
        {
            public const string Register = "Employee/Register";
            public const string Update = "Employee/Update";
            public const string Delete = "Employee/Delete";
        }
    }
}
