namespace LibriGenie.Api.Models;

public class Task
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string Category { get; set; }
    public bool EnableWordpress { get; set; }
    public string? UsernameWordpress { get; set; }
    public string? PasswordWordpress { get; set; }
}
