using Microsoft.AspNetCore.Mvc;
using ObservabilityDns.Api.Services;
using ObservabilityDns.Contracts.DTOs;

namespace ObservabilityDns.Api.Controllers;

[ApiController]
[Route("api/groups")]
public class GroupsController : ControllerBase
{
    private readonly GroupService _groupService;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(GroupService groupService, ILogger<GroupsController> logger)
    {
        _groupService = groupService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<GroupDto>>> GetAllGroups()
    {
        var groups = await _groupService.GetAllGroupsAsync();
        return Ok(groups);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GroupDetailDto>> GetGroup(Guid id)
    {
        var group = await _groupService.GetGroupByIdAsync(id);
        if (group == null)
            return NotFound();

        return Ok(group);
    }

    [HttpPost]
    public async Task<ActionResult<GroupDto>> CreateGroup([FromBody] CreateGroupRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var group = await _groupService.CreateGroupAsync(request);
            return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, group);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error creating group {GroupName}", request.Name);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group {GroupName}", request.Name);
            return StatusCode(500, new { error = "Failed to create group", message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] UpdateGroupRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await _groupService.UpdateGroupAsync(id, request);
            if (!updated)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error updating group {GroupId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group {GroupId}", id);
            return StatusCode(500, new { error = "Failed to update group", message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGroup(Guid id, [FromQuery] bool unassignDomains = true)
    {
        var deleted = await _groupService.DeleteGroupAsync(id, unassignDomains);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    [HttpPost("{groupId}/domains/{domainId}")]
    public async Task<IActionResult> AssignDomainToGroup(Guid groupId, Guid domainId)
    {
        var assigned = await _groupService.AssignDomainToGroupAsync(domainId, groupId);
        if (!assigned)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{groupId}/domains/{domainId}")]
    public async Task<IActionResult> UnassignDomainFromGroup(Guid groupId, Guid domainId)
    {
        var unassigned = await _groupService.AssignDomainToGroupAsync(domainId, null);
        if (!unassigned)
            return NotFound();

        return NoContent();
    }

    [HttpPost("domains/{domainId}/assign")]
    public async Task<IActionResult> AssignDomainToGroupId([FromRoute] Guid domainId, [FromBody] AssignDomainToGroupRequest request)
    {
        var assigned = await _groupService.AssignDomainToGroupAsync(domainId, request.GroupId);
        if (!assigned)
            return NotFound();

        return NoContent();
    }
}

public class AssignDomainToGroupRequest
{
    public Guid? GroupId { get; set; }
}
