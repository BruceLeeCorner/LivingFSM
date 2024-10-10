using System;

namespace LivingFSM
{
    public interface ITransitionTable
    {
        bool CanMatch(Enum msgCmd);

        bool CanMatch(Enum msgCmd, Enum state);

        bool CanMatch(Enum msgCmd, Enum state, out Func<object[], bool> action, out Enum nextState);

        bool CanMatch(Enum msgCmd, out Func<object[], bool> action, out Enum nextState);
    }
}