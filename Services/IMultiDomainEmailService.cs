namespace DigitalWonderlab.MultiDomainEmail.Services
{
    public interface IMultiDomainEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody, string textBody = null);
        Task SendEmailAsync(string to, string subject, string htmlBody, string textBody = null, string fromDomain = null);
    }
}
