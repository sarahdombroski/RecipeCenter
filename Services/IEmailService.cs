namespace TeamProjectYay.Services;  // For abstraction and Dependency Injection

public interface IEmailService
{
    Task SendInviteEmailAsync(string toEmail, string inviterName); // Implementation in EmailService.cs
}
