public enum LockType { None, Code, Item }

public interface ILockableDoor
{
    string RequiredCode { get; }
    bool IsLocked { get; }
    void Unlock();
    void CloseAndLock();
}