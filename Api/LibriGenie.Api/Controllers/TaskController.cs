using LibriGenie.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibriGenie.Api.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public class TaskController : ControllerBase
{
    private readonly ITaskService taskService;

    public TaskController(ITaskService taskService)
    {
        this.taskService = taskService;
    }

    [HttpGet("[action]")]
    public async Task<IActionResult> GetTasksForRun(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var tasks = await taskService.GetTasksForRun(page, pageSize, cancellationToken);  
        
        return Ok(tasks);
    }

    [HttpGet("[action]")]
    public async Task<IActionResult> GetAllActiveTasks(CancellationToken cancellationToken = default)
    {
        var tasks = await taskService.GetAllActiveTasks(cancellationToken);
        
        return Ok(tasks);
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> SetLastRun(string id, CancellationToken cancellationToken = default)
    {
        await taskService.SetLastRun(id, cancellationToken);

        return Ok();
    }
}
