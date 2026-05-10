public enum AuditLogEvent
{
    LoginSuccess,
    LoginFailed,
    AccountLocked,
    TokenRefreshed,
    Logout,
    LogoutAllDevices,
    PasswordResetRequested,
    PasswordResetCompleted,
    TwoFactorSetup,
    TwoFactorSuccess,
    TwoFactorFailed,
    EmailConfirmed,
    UserRegistered
}
