namespace chat_dotnet.Models.DTOs;

public class GroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
}

public class CreateGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<int> MemberIds { get; set; } = new List<int>();
}

public class GroupMemberDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsOnline { get; set; }
    public DateTime JoinedAt { get; set; }
}
