namespace TaskFlowWebApplication.Models
{
    public class Admin
    {
        public int AdminID {  get; set; }
        public string AdminName { get; set; }
        protected string AdminPassword { get; set; }
        protected int UserID { get; set; }

    }
}
