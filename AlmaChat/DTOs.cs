using System.Text.Json.Serialization;

namespace AlmaChat;

// ================== DTOs ==================
public class UserDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("username")] public string Username { get; set; }
}

public class ParticipantDto
{
    [JsonPropertyName("userId")] public string UserId { get; set; } 
    [JsonPropertyName("username")] public string Username { get; set; }
}

public class ChatDto 
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("is_group")] public bool IsGroup { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("participants")] public List<ParticipantDto> Participants { get; set; } = new();

    public string DisplayName(long myId) 
    {
        if (IsGroup && !string.IsNullOrWhiteSpace(Name)) return Name;
        var other = Participants.FirstOrDefault(p => p.UserId != myId.ToString());
        return other?.Username ?? "Chat";
    }
    
    public long GetOtherUserId(long myId)
    {
        if (IsGroup) return 0;
        var other = Participants.FirstOrDefault(p => p.UserId != myId.ToString());
        return other != null && long.TryParse(other.UserId, out long id) ? id : 0;
    }
}

public class MessageDto 
{ 
    [JsonPropertyName("id")] public long Id { get; set; } 
    [JsonPropertyName("sender_id")] public long SenderId { get; set; } 
    [JsonPropertyName("content")] public string Content { get; set; } 
    [JsonPropertyName("sender")] public string? SenderName { get; set; } 
}

public class UserProfileDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("username")] public string Username { get; set; } = "";
    [JsonPropertyName("email")] public string Email { get; set; } = "";
    [JsonPropertyName("first_name")] public string? FirstName { get; set; }
    [JsonPropertyName("last_name")] public string? LastName { get; set; }
    [JsonPropertyName("bio")] public string? Bio { get; set; }
}

public class GitHubReleaseDto
{
    [JsonPropertyName("tag_name")] public string TagName { get; set; } = "";
    [JsonPropertyName("html_url")] public string HtmlUrl { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}