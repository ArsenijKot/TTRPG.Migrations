public class Member
{
    public int MemberId { get; set; }
    public string Name { get; set; } = null!;
    public string? Nickname { get; set; }
    public string Email { get; set; } = null!;
    public DateTime JoinDate { get; set; }
}
