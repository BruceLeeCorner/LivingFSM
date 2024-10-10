using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LivingFSM
{
    public class StateMachine : IStateMachine, ITransitionTable, IStateTracker
    {
        private CancellationTokenSource _cancellationTokenSource;

        private Enum _currState;

        private LinkedList<Enum> _historyStates = new LinkedList<Enum>();

        private Queue<LongTimeActionBase> _longTimeActions = new Queue<LongTimeActionBase>();

        private BlockingCollection<(Enum msgCmmd, object[] args)> _msgQueue;

        private bool _pauseFlag;

        private Dictionary<string, List<(Enum msgCmd, Func<object[], bool> action, Enum nextState)>> _transitionTable;

        private int interval = 100;

        public StateMachine()
        {
            _transitionTable = new Dictionary<string, List<(Enum msg, Func<object[], bool> action, Enum nextState)>>();
            _msgQueue = new BlockingCollection<(Enum msgCmd, object[] msgArgs)>();
            Loop();
        }

        public event EventHandler<StateTransitedEventArgs> MsgNotMatchAnyState;

        public event EventHandler<StateTransitedEventArgs> StateEntered;

        public event EventHandler<StateTransitedEventArgs> StateExited;
        Enum IStateTracker.CurrState => throw new NotImplementedException();

        //    return Result.Forward;
        //}
        int IStateTracker.HistoryStateCapacity => throw new NotImplementedException();

        //    Result ret = Result.Finished;
        //    var lst = _longTimeActions.ToList();
        //    for (int i = 0; i < lst.Count; i++)
        //    {
        //        ret = lst[i].Start();
        //        if (ret == Result.Finished)
        //        {
        //            _longTimeActions.Dequeue();
        //            continue;
        //        }
        //        else
        //        {
        //            break;
        //        }
        //    }
        Enum IStateTracker.PrevState { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        //public Result StartAction()
        //{
        //    if (_longTimeActions.Count == 0)
        //        return Result.Finished;
        public object Tag { get; set; }

        protected Queue<LongTimeActionBase> QueueActions
        {
            get { return _longTimeActions; }
        }

        public void AbortAction()
        {
            _longTimeActions.Peek().Abort(null);
            _longTimeActions.Clear();
        }

        //public Result MonitorAction()
        //{
        //    if (_longTimeActions.Count == 0)
        //        return Result.Finished;

        //    LongTimeActionBase routine = _longTimeActions.Peek();

        //    var ret = routine.Steps();
        //    if (ret.Result == Result.Finished)
        //    {
        //        _longTimeActions.Dequeue();

        //        var lst = _longTimeActions.ToList();
        //        for (int i = 0; i < lst.Count; i++)
        //        {
        //            ret = lst[i].Start();
        //            if (ret == Result.Finished)
        //            {
        //                _longTimeActions.Dequeue();
        //                continue;
        //            }
        //            else
        //            {
        //                break;
        //            }
        //        }
        //    }

        //    return ret;
        //}

        //public Result StartAction(LongTimeActionBase action, params object[] args)
        //{
        //    QueueActions.Clear();

        //    QueueActions.Enqueue(action);

        //    return QueueActions.Peek().Start(args);
        //}

        //public Result StartAction(LongTimeActionBase action)
        //{
        //    QueueActions.Clear();

        //    QueueActions.Enqueue(action);

        bool ITransitionTable.CanMatch(Enum msgCmd)
        {
            return ((ITransitionTable)this).CanMatch(msgCmd, _currState);
        }

        bool ITransitionTable.CanMatch(Enum msgCmd, Enum state)
        {
            var anyHashCode = FsmState.Any.GetHashStringCode();
            if (_transitionTable.ContainsKey(anyHashCode) && _transitionTable[anyHashCode].Any(item => item.msgCmd.IsSame(msgCmd)))
                return true;
            var stateHashCode = state.GetHashStringCode();
            if (_transitionTable.ContainsKey(stateHashCode) && _transitionTable[stateHashCode].Any(item => item.msgCmd.IsSame(msgCmd)))
                return true;
            return false;
        }

        bool ITransitionTable.CanMatch(Enum msgCmd, Enum state, out Func<object[], bool> action, out Enum nextState)
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

        bool ITransitionTable.CanMatch(Enum msgCmd, out Func<object[], bool> action, out Enum nextState) => ((ITransitionTable)this).CanMatch(msgCmd, _currState, out action, out nextState);

        //    return QueueActions.Peek().Start();
        //}
        public void Continue()
        {
            _pauseFlag = true;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        Enum IStateTracker.GetState(int prevIndex)
        {
            return _historyStates.ElementAt(prevIndex);
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
            while (_pauseFlag)
            {
                var success = _msgQueue.TryTake(out (Enum cmd, object[] args) msg, interval, _cancellationTokenSource.Token);

                if (!success)
                {
                    msg = (FsmMsgCmd.Timer, null);
                }
                // 不管当前是什么状态，全部忽略之，强制执行switch default 对应的逻辑，并强制切换到指定状态。换句话说，任何状态都要处理此消息，执行action后，跳转到指定的状态。
                var match = ((ITransitionTable)this).CanMatch(msg.cmd, out Func<object[], bool> action, out Enum nextState);
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