namespace switter;

public class ApplicationUser
{
    public string UserName { get; set; }
    public DateTime CreationDate { get; set; }
    public List<string> PostIDs { get; set; }
    public int Points { get; set; }
}