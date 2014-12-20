// ReSharper disable once CheckNamespace
namespace System.Threading.Tasks
{
    static class TaskEx
    {
        static readonly Task _completed = Task.FromResult(0);
        public static Task Completed()
        {
            return _completed;
        }
    }
}
