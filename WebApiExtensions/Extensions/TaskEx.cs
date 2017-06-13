// ReSharper disable once CheckNamespace
namespace System.Threading.Tasks
{
    static class TaskEx
    {
        public static Task CompletedTask { get; } = Task.FromResult(0);
    }
}
