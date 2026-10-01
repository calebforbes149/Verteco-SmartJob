namespace VertecoSmartJob.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
}
