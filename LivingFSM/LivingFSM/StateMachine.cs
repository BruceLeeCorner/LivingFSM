using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LivingFSM
{
    public class StateMachine : IStateMachine
    {
        private CancellationTokenSource _cancellationTokenSource;

        private Enum _currState;
        private ManualResetEventSlim _pauseEvent;

        private LinkedList<Enum> _historyStates = new LinkedList<Enum>();

        private Queue<LongTimeActionBase> _longTimeActions = new Queue<LongTimeActionBase>();

        private BlockingCollection<(Enum msgCmmd, object[] args)> _msgQueue;

        private Dictionary<string, List<(Enum msgCmd, Func<object[], bool> action, Enum nextState)>> _transitionTable;

        private int interval = 100;
        public int Interval
        {
            get => interval;
            set
            {
                interval = value;
            }
        }

        public StateMachine() : this(FsmState.Pangu)
        {

        }

        public StateMachine(Enum initialState)
        {
            _currState = initialState;
            _transitionTable = new Dictionary<string, List<(Enum msg, Func<object[], bool> action, Enum nextState)>>();
            _msgQueue = new BlockingCollection<(Enum msgCmd, object[] msgArgs)>();
            _pauseEvent = new ManualResetEventSlim(true);
            new Thread(Loop).Start();
        }

        public event EventHandler<StateTransitedEventArgs> MsgNotMatchAnyState;

        public event EventHandler<StateTransitedEventArgs> StateEntered;

        public event EventHandler<StateTransitedEventArgs> StateExited;

        #region IStateTracker
        Enum IStateMachine.CurrState => _currState;
        Enum IStateMachine.PrevState => ((IStateMachine)this).GetState(1);
        int IStateMachine.HistoryStateCapacity => throw new NotImplementedException();
        Enum IStateMachine.GetState(int prevIndex)
        {
            return _historyStates.ElementAt(prevIndex);
        }
        #endregion

        public object Tag { get; set; }

        bool IStateMachine.CanMatch(Enum msgCmd)
        {
            return ((IStateMachine)this).CanMatch(msgCmd, _currState);
        }

        bool IStateMachine.CanMatch(Enum msgCmd, Enum state)
        {
            var anyHashCode = FsmState.Any.GetHashStringCode();
            if (_transitionTable.ContainsKey(anyHashCode) && _transitionTable[anyHashCode].Any(item => item.msgCmd.IsSame(msgCmd)))
                return true;
            var stateHashCode = state.GetHashStringCode();
            if (_transitionTable.ContainsKey(stateHashCode) && _transitionTable[stateHashCode].Any(item => item.msgCmd.IsSame(msgCmd)))
                return true;
            return false;
        }

        bool IStateMachine.CanMatch(Enum msgCmd, Enum state, out Func<object[], bool> action, out Enum nextState)
        {
            action = default;
            nextState = default;

            var anyHashCode = FsmState.Any.GetHashStringCode();
            if (_transitionTable.ContainsKey(anyHashCode))
            {
                var index = _transitionTable[anyHashCode].FindIndex(item => item.msgCmd.IsSame(msgCmd));
                if (index > -1)
                {
                    action = _transitionTable[anyHashCode][index].action;
                    nextState = _transitionTable[anyHashCode][index].nextState;
                    return true;
                }
            }
            var stateHashCode = state.GetHashStringCode();
            if (_transitionTable.ContainsKey(stateHashCode))
            {
                var index = _transitionTable[stateHashCode].FindIndex(item => item.msgCmd.IsSame(msgCmd));
                if (index > -1)
                {
                    action = _transitionTable[stateHashCode][index].action;
                    nextState = _transitionTable[stateHashCode][index].nextState;
                    return true;
                }
            }
            return false;
        }

        bool IStateMachine.CanMatch(Enum msgCmd, out Func<object[], bool> action, out Enum nextState) => ((IStateMachine)this).CanMatch(msgCmd, _currState, out action, out nextState);

        public void Resume()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _pauseEvent.Set();
        }

        public void Pause()
        {
            _pauseEvent.Reset();
            _cancellationTokenSource.Cancel();
        }

        public void PostMsg(Enum msgCmd, params object[] args)
        {
            _msgQueue.Add((msgCmd, args));
        }

        public void Register(Enum currState, Enum nextState, Enum msgCmd, Func<object[], bool> action)
        {
            var stateHashCode = currState.GetHashStringCode();
            if (!_transitionTable.ContainsKey(stateHashCode))
            {
                _transitionTable[stateHashCode] = new List<(Enum msg, Func<object[], bool> action, Enum nextState)>();
            }
            _transitionTable[stateHashCode].Add((msgCmd, action, nextState));
        }

        public void Register(Enum currState, Enum nextState, Enum msgCmd)
        {
            Register(currState, nextState, msgCmd, (object[] args) => true);
        }

        public void Register(Enum currState, Enum nextState, Enum msgCmd, Enum relayState, LongTimeActionBase longTimeAction)
        {
            Register(currState, relayState, msgCmd, (args) =>
            {
                ((IReceiver)longTimeAction).RecvArgs(args);
                var can = longTimeAction.CanAction();
                if (!can) { longTimeAction.OnCannotAction(); }
                return can;
            });

            Register(relayState, currState, FsmMsgCmd.Abort, (args) =>
            {
                longTimeAction.Abort(longTimeAction.StepResultToken.Parameter);
                return true;
            });

            Register(relayState, nextState, FsmMsgCmd.Timer, (args) =>
            {
                var result = longTimeAction.Steps();

                // Long Time Action Finished.
                switch (result.Result)
                {
                    case Result.Finished:
                        longTimeAction.Finished(args);
                        return true;

                    case Result.Forward:
                        longTimeAction.Forward(args);
                        ; // Do Nothing
                        return false;

                    case Result.Failed:
                        return longTimeAction.Failed(longTimeAction.StepResultToken.Parameter);
                }
                throw new ArgumentException();
            });
        }

        private void Loop()
        {
            while (true)
            {
                _pauseEvent.Wait(); // 暂停机制
                bool success; (Enum cmd, object[] args) msg;
                try { success = _msgQueue.TryTake(out msg, interval, _cancellationTokenSource.Token); }
                catch (OperationCanceledException) { continue; }

                if (success == false)
                {
                    msg = (FsmMsgCmd.Timer, null);
                }
                // 不管当前是什么状态，全部忽略之，强制执行switch default 对应的逻辑，并强制切换到指定状态。换句话说，任何状态都要处理此消息，执行action后，跳转到指定的状态。
                var match = ((IStateMachine)this).CanMatch(msg.cmd, out Func<object[], bool> action, out Enum nextState);
                // 当前状态不接受此消息
                if (!match)
                {
                    if (!msg.cmd.IsSame(FsmMsgCmd.Timer))
                    {
                        MsgNotMatchAnyState?.Invoke(this, new StateTransitedEventArgs(msg.cmd, _currState));
                    }
                    continue;
                }

                // Execute Action
                var result = action.Invoke(msg.args);

                // 成功切换状态
                if (result)
                {
                    StateExited?.Invoke(this, new StateTransitedEventArgs(msg.cmd, _currState));
                    _currState = nextState;
                    StateEntered?.Invoke(this, new StateTransitedEventArgs(msg.cmd, _currState));
                }
            }
        }
    }
}