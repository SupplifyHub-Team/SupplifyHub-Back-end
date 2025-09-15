using API.DTO;

namespace API.Services
{
    public interface IEmailService
    {
        void SendEmail(EmailDto request);
    }
}
