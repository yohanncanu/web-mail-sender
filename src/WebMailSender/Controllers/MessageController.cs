using MailLib.Configuration;
using MailLib.Model;
using MailLib.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace WebMailSender.Controllers;

[Route("api/messages")]
public class MessageController : ApplicationController
{
    private readonly ILogger<MessageController> _logger;
    private readonly EmailSender _service;
    private readonly MailSettings _mailSettings;

    public MessageController(ILogger<MessageController> logger, EmailSender service, IOptions<MailSettings> mailSettings)
    {
        _logger = logger;
        _service = service;
        _mailSettings = mailSettings.Value;
    }


    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult SendEmail([FromBody] EmailDefinition body)
    {
        _service.SendEmails(_mailSettings, body);
        return Ok();
    }
}
