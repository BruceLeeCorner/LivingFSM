using System;

namespace LivingFSM
{
    public interface IStateMachine
    {
        void Resume();

        void Pause();

        #region TransitionTable

        bool CanMatch(Enum msgCmd);

        bool CanMatch(Enum msgCmd, Enum state);

        bool CanMatch(Enum msgCmd, Enum state, out Func<object[], bool> action, out Enum nextState);

        bool CanMatch(Enum msgCmd, out Func<object[], bool> action, out Enum nextState);

        #endregion TransitionTable

        #region StateTracker

        Enum CurrState { get; }
        int HistoryStateCapacity { get; }
        Enum PrevState { get; }

        Enum GetState(int prevIndex);

        #endregion StateTracker
    }
}