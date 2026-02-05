public class MemberSessionDto
{
    public int MemberId { get; set; }
    public string MemberName { get; set; } = null!;
    public string? Nickname { get; set; }
    public string GameTitle { get; set; } = null!;
    public DateTime SessionDate { get; set; }
}
