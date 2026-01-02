using FluentEmail.Core;

namespace TeamProjectYay.Services;

public class EmailService : IEmailService // Implementation of IEmailService
{
    private readonly IFluentEmail _fluentEmail; // instance of FluentEmail

    public EmailService(IFluentEmail fluentEmail)  // Dependency Injection of FluentEmail
    {
        _fluentEmail = fluentEmail;
    }

    public async Task SendInviteEmailAsync(string toEmail, string inviterName)
    {
        var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #4a79f8 0%, #83cf89 100%); padding: 30px; border-radius: 10px 10px 0 0; text-align: center;'>
        <h2 style='color: white; margin: 0;'>Let's Cook My Friend!</h2>
    </div>
    <div style='background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;'>
        <p style='font-size: 16px;'>Hello!</p>
        <p style='font-size: 16px;'><strong>{inviterName}</strong> has invited you to Recipe Center - a recipe sharing community. The grill's hot!</p>
        <div style='text-align: center; margin: 30px 0;'>
            <p>There is no active working link for this website. Please go into your EmailService.cs and update the link</p>
            <a href='' 
               style='background: #4a79f8; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;'>
                Join Now
            </a>
        </div>
        <p style='font-size: 14px; color: #666;'>If you're not interested, you can safely ignore this email.</p>
    </div>
    <div style='text-align: center; padding: 20px; font-size: 12px; color: #999;'>
        <p>This is an invitation sent on behalf of {inviterName}</p>
        <p>&copy; 2025 Do More Cook More. All rights reserved.</p>
    </div>
</body>
</html>";

        var plainTextBody = $@"
Let's Cook My Friend!

Hello!

{inviterName} has invited you to join Do More Cook More - a recipe sharing community. The grill's hot!

Join now: There is no working link right now. Please go into EmailService.cs and update the link.

If you're not interested, you can safely ignore this email.

---
This is an invitation sent on behalf of {inviterName}
© 2025 Do More Cook More. All rights reserved.
";

            await _fluentEmail 
                .To(toEmail)
                .Subject($"{inviterName} invited you to join Do More Cook More")
                .Body(emailBody, isHtml: true)
                .PlaintextAlternativeBody(plainTextBody)
                .SendAsync();
        }
}
