using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace Game
{
    public class ProcedureET : ProcedureBase
    {
#if UNITY_ET
        private bool m_Started;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            m_Started = false;
            if (RuntimeInstanceContext.State == RuntimeInstanceState.Failed)
            {
                Log.Warning(RuntimeInstanceContext.Failure);
                return;
            }
            // 产品能力在既有 Loader 装载后由 Entry 核对；字节模式不能按未装载程序集误拒绝。
            try
            {
                GameEntry.CodeRunner.StartRun("ET.Init");
                m_Started = true;
            }
            catch (System.Exception e)
            {
                RuntimeInstanceContext.Fail(GameFramework.Utility.Text.Format("ETEntryFailed: {0}", e.Message));
                throw;
            }
            Log.Info("Start run ET!");
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            StopOwnedEntry();
            base.OnLeave(procedureOwner, isShutdown);
        }
#endif

        public void StopOwnedEntry()
        {
#if UNITY_ET
            if (m_Started)
            {
                m_Started = false;
                GameEntry.CodeRunner.StopRun();
            }
#endif
            RuntimeInstanceContext.Stop();
        }
    }
}
