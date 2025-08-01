using LibriGenie.Api.Models;
using LibriGenie.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibriGenie.Api.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public class EmailController : ControllerBase
{
    private readonly IMailService mailService;

    public EmailController(IMailService mailService)
    {
        this.mailService = mailService;
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Send(EmailRequest emailRequest, CancellationToken cancellationToken = default)
    {
        await mailService.Send(emailRequest.To, emailRequest.Subject, emailRequest.Body, cancellationToken);

        return Ok();
    }
}
