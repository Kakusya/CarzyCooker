using System;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static void ResetCompanionDisplay(this GridPresentationComponent self)
        {
            self.CompanionChat = CompanionChatTopic.None;
            self.CompanionWorld = null;
            self.CompanionRole = 0;
            self.CompanionFeedback = null;
            self.CompanionDisplayedCount = 0;
            ++self.CompanionFeedbackRevision;
        }

        private static void ObserveCompanion(this GridPresentationComponent self, GridClientComponent grid)
        {
            if (grid.LevelPhase != LevelPhase.Preparing)
            {
                self.CompanionChat = CompanionChatTopic.None;
            }
            foreach (GridCompanionProjection role in grid.Companions.Values)
            {
                bool baseline = self.CompanionWorld != grid.WorldId || self.CompanionWorldGeneration != grid.WorldGeneration ||
                    self.CompanionLevel != grid.LevelId || self.CompanionLevelGeneration != grid.LevelGeneration || self.CompanionRole != role.Id;
                if (baseline)
                {
                    self.ResetCompanionDisplay();
                    self.CompanionWorld = grid.WorldId;
                    self.CompanionWorldGeneration = grid.WorldGeneration;
                    self.CompanionLevel = grid.LevelId;
                    self.CompanionLevelGeneration = grid.LevelGeneration;
                    self.CompanionRole = role.Id;
                }
                else if (role.CompletedCount > self.CompanionDisplayedCount)
                {
                    DRCompanionConfig definition = Tables.Instance.DTCompanionConfig.Get(role.DefinitionId);
                    self.CompanionFeedback = role.Name + "：" + definition.WorkFeedback;
                    if (self.CompanionDisplayedCount < 3 && role.CompletedCount >= 3)
                    {
                        self.CompanionFeedback += " " + definition.GrowthFeedback;
                    }
                    ++self.CompanionFeedbackRevision;
                }
                self.CompanionDisplayedCount = role.CompletedCount;
                return;
            }
            self.ResetCompanionDisplay();
        }

        private static void RefreshCompanionInformation(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (self.CompanionChat == CompanionChatTopic.None || grid.LevelPhase != LevelPhase.Preparing)
            {
                return;
            }
            foreach (GridCompanionProjection role in grid.Companions.Values)
            {
                DRCompanionConfig definition = Tables.Instance.DTCompanionConfig.Get(role.DefinitionId);
                string text = self.CompanionChat switch
                {
                    CompanionChatTopic.Preparation => definition.PreparationChat,
                    CompanionChatTopic.Cooperation => definition.CooperationChat,
                    CompanionChatTopic.Closing => definition.ClosingChat,
                    _ => ""
                };
                view.SetInformation(role.Name, role.Name + "：" + text, true, self.OrdersVisible);
                return;
            }
        }

        private static void AddCompanionTargets(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (grid.LevelPhase != LevelPhase.Preparing || grid.Companions.Count == 0)
            {
                return;
            }
            EntityRef<GridPresentationComponent> original = self;
            long epoch = self.HudLoadEpoch, level = grid.LevelId, generation = grid.LevelGeneration, worldGeneration = grid.WorldGeneration;
            string world = grid.WorldId;
            void Select(CompanionChatTopic topic)
            {
                GridPresentationComponent current = original;
                if (current == null || !current.Ready || current.HudLoadEpoch != epoch)
                {
                    return;
                }
                var client = ((Scene)current.IScene).GetComponent<ClientSessionComponent>();
                GridClientComponent latest = client?.GetComponent<GridClientComponent>();
                if (latest == null || latest.WorldId != world || latest.WorldGeneration != worldGeneration || latest.LevelId != level ||
                    latest.LevelGeneration != generation || latest.LevelPhase != LevelPhase.Preparing)
                {
                    return;
                }
                current.CompanionChat = topic;
                current.RecipeVisible = false;
                current.RefreshGridHud();
            }
            view.AddTarget("聊聊备菜", () => Select(CompanionChatTopic.Preparation));
            view.AddTarget("聊聊合作", () => Select(CompanionChatTopic.Cooperation));
            view.AddTarget("聊聊收店", () => Select(CompanionChatTopic.Closing));
            if (self.CompanionChat != CompanionChatTopic.None)
            {
                view.AddTarget("结束聊天", () => Select(CompanionChatTopic.None));
            }
        }
    }
}
