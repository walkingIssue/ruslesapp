public partial class MunicipalityHub
{
    [Flags]
    public enum ConnectionFault { ok= 0, fail = 1, DoesntExist = 2, OutofOrder =4 } 
}
