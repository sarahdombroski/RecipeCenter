using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using TeamProjectYay.Models;
using TeamProjectYay.Services;

namespace TeamProjectYay.Components.Pages;

public partial class Invite
{
    [Inject]
    private IEmailService EmailService { get; set; } = default!;

    [Required]   
    [StringLength(60, MinimumLength = 3)]
    private string inviterName = string.Empty;

    [Required] 
    [EmailAddress]
    private InviteEmail inviteEmailM = new();
    private bool emailSent = false;
    private string errorMessage = string.Empty;

    private async Task SendInvite()
    {
        // Reset state
        emailSent = false;
        errorMessage = string.Empty;

        // Validate input
        if (string.IsNullOrWhiteSpace(inviterName))
        {
            errorMessage = "Please enter your name.";
            return;
        }

        if (string.IsNullOrWhiteSpace(inviteEmailM.UserEmail))
        {
            errorMessage = "Please enter an email address.";
            return;
        }

        try
        {
            await EmailService.SendInviteEmailAsync(inviteEmailM.UserEmail, inviterName);
            emailSent = true;
            
            // Reset form
            inviterName = string.Empty;
            inviteEmailM = new();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to send invitation: {ex.Message}";
        }
    }
}
