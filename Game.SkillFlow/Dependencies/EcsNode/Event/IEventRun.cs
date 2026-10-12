using System;
using System.Threading.Tasks;

namespace ECS
{
    public interface IDomainEvent
    {
    }

    public interface IEventRun
    {
        public Task Handle(EcsNode domain, IDomainEvent a);
    }

    public abstract class AEventRun<T, A> : IEventRun where T : EcsNode where A : IDomainEvent
    {
        protected abstract Task Run(T domain, A a);
        public async Task Handle(EcsNode domain, IDomainEvent a)
        {
            try
            {
                await Run((T)domain, (A)a);
            }
            catch (Exception e)
            {
                ConsoleLog.Error(e);
            }
        }
    }
}
