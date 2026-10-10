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
            RuntimeInstanceConfig config = RuntimeInstanceContext.Config;
            if (config != null && config.Role != RuntimeInstanceRole.Legacy)
            {
                RuntimeInstanceContext.Fail("ProductBootstrapUnavailable");
                Log.Warning("ProductBootstrapUnavailable");
                return;
            }
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
