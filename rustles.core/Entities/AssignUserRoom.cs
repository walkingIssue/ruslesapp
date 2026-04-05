namespace rustles.core.Entities
{
    public class AssignUserRoom
    {
        public Guid UserId { get; set; }
        public Guid MunipId { get; set; }
        public RequestPosition RequestPosition { get; }
        public Position Position { get; }
        public string[] NaturalName { get; }
    }
}