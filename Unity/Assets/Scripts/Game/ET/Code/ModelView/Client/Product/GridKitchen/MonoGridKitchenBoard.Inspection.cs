using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    public partial class MonoGridKitchenBoard
    {
        // 普通表现子节点，归本Board本轮view；不是实体或独立资源池。
        private readonly List<SpriteRenderer> stationInspectionMarkers = new(16);
        private long inspectionStationId;
        private IReadOnlyList<GridCell> inspectionFootprint, inspectionApproaches;

        public void SetStationInspection(GridStationProjection? station)
        {
            if (!configured)
            {
                return;
            }
            if (!station.HasValue)
            {
                if (inspectionStationId == 0)
                {
                    return;
                }
                foreach (SpriteRenderer marker in stationInspectionMarkers)
                {
                    marker.gameObject.SetActive(false);
                }
                inspectionStationId = 0; inspectionFootprint = inspectionApproaches = null;
                return;
            }
            GridStationProjection current = station.Value;
            if (inspectionStationId == current.Id && SameCells(inspectionFootprint, current.Footprint) && SameCells(inspectionApproaches, current.Approaches))
            {
                return;
            }
            int used = 0;
            foreach (GridCell cell in current.Footprint) { MarkStationInspection(cell, true, used++); }
            foreach (GridCell cell in current.Approaches) { MarkStationInspection(cell, false, used++); }
            for (int index = used; index < stationInspectionMarkers.Count; ++index)
            {
                stationInspectionMarkers[index].gameObject.SetActive(false);
            }
            inspectionStationId = current.Id; inspectionFootprint = current.Footprint; inspectionApproaches = current.Approaches;
        }

        private void MarkStationInspection(GridCell cell, bool footprint, int index)
        {
            SpriteRenderer marker;
            if (index == stationInspectionMarkers.Count)
            {
                marker = CreateSprite("Station inspection", Vector3.zero, Vector3.one, Color.white, 4500);
                stationInspectionMarkers.Add(marker);
            }
            else
            {
                marker = stationInspectionMarkers[index];
            }
            marker.name = footprint ? "Station footprint " + index : "Station approach " + index;
            marker.transform.localPosition = CellToWorld(cell);
            marker.transform.localScale = footprint ? new Vector3(.85f, .85f * GroundY, 1) : new Vector3(.22f, .22f * GroundY, 1);
            marker.color = footprint ? new Color(.2f, .55f, 1f, .26f) : new Color(.25f, .95f, .55f, .95f);
            marker.gameObject.SetActive(true);
        }

        private void ReleaseStationInspection()
        {
            stationInspectionMarkers.Clear(); inspectionStationId = 0; inspectionFootprint = inspectionApproaches = null;
        }
    }
}
