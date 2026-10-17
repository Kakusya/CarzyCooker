using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    public partial class MonoGridKitchenBoard
    {
        private readonly HashSet<int> pingMembers = new();
        private readonly HashSet<int> visiblePingMembers = new();
        private readonly Dictionary<int, string> pingNames = new();

        public void BeginTargetPings() => visiblePingMembers.Clear();

        public void ShowTargetPing(int memberId, GridCell cell, string targetName)
        {
            // Real entity label IDs are positive; this key is only local visual bookkeeping.
            long labelKey = -(long)memberId;
            Vector3 position = CellToWorld(cell) + new Vector3((memberId % 2 == 0 ? .25f : -.25f), .8f, 0);
            Text caption = Label(labelKey, position);
            caption.transform.parent.localPosition = position;
            if (!pingNames.TryGetValue(memberId, out string previousName) || previousName != targetName)
            {
                pingNames[memberId] = targetName;
                caption.text = "成员" + memberId + " → " + targetName;
                caption.color = new Color(.72f, .12f, .58f);
                caption.fontStyle = FontStyle.Bold;
                caption.transform.parent.GetComponent<Canvas>().sortingOrder = 6500;
            }
            pingMembers.Add(memberId); visiblePingMembers.Add(memberId);
        }

        public void EndTargetPings()
        {
            removedMembers.Clear();
            foreach (int member in pingMembers)
            {
                if (!visiblePingMembers.Contains(member))
                {
                    RemoveLabel(-(long)member); removedMembers.Add(member);
                }
            }
            foreach (int member in removedMembers)
            {
                pingMembers.Remove(member);
                pingNames.Remove(member);
            }
        }

        public void ClearTargetPings()
        {
            foreach (int member in pingMembers)
            {
                RemoveLabel(-(long)member);
            }
            ReleaseTargetPings();
        }

        private void ReleaseTargetPings()
        {
            pingMembers.Clear(); visiblePingMembers.Clear(); pingNames.Clear();
        }
    }
}
