namespace SH.Framework.Library.Cqrs;

public interface IHasRequestId
{
    public Guid RequestId();
}